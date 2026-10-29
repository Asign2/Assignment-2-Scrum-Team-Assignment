using System;
using System.Collections.Generic;
using System.Linq;
using Assign_2;
using Microsoft.Data.SqlClient;
using Xunit;

namespace SprintUnitTests;

// Extra database integration tests. Keep all existing Sprint tests unchanged.
// Requires the same LocalDB setup as the existing tests.
// Every inserted sensor/location and user is identified by a unique test marker.
public class AdditionalUserDatabaseTests : IDisposable
{
    private readonly List<(int Floor, int Room)> _locationsToClean = new();
    private readonly List<int> _usersToClean = new();

    private static int NewFloor() => -1000000 - Random.Shared.Next(1, 100000000);
    private static string UniqueUser() => "dbextra_" + Guid.NewGuid().ToString("N")[..12];

    private (int Floor, int Room) UniqueLocation()
    {
        var result = (Floor: NewFloor(), Room: Random.Shared.Next(10000, 99999));
        _locationsToClean.Add(result);
        return result;
    }

    private SensorRecord AddTestSensor(bool active, int variance)
    {
        var place = UniqueLocation();
        Assert.True(SensorsDatabase.Register("CoverageMake", "CoverageModel", place.Floor, place.Room, active, variance));
        return Assert.Single(SensorsDatabase.GetAllSensors(),
            s => s.Floor == place.Floor && s.Room == place.Room && s.Make == "CoverageMake");
    }

    private (int Id, string Username) AddTestUser(string role = "User")
    {
        string username = UniqueUser();
        Assert.True(UserDatabase.Register(username, "CoveragePass123!", role, "Coverage", "Tester", ""));
        var user = Assert.Single(UserDatabase.GetAllUsers(), u => u.Username == username);
        _usersToClean.Add(user.Id);
        return (user.Id, username);
    }

    public void Dispose()
    {
        // Only remove objects created by THIS test instance. Do not call ResetDatabase().
        foreach (int id in _usersToClean)
            UserDatabase.DeleteUser(id);

        using var connection = UserDatabase.OpenConnection();
        foreach (var location in _locationsToClean)
        {
            // The existing schema uses ON DELETE CASCADE from Locations to Sensors
            // and from Sensors to Data.
            using var command = new SqlCommand(
                "DELETE FROM dbo.Locations WHERE Floor = @floor AND Room = @room;", connection);
            command.Parameters.AddWithValue("@floor", location.Floor);
            command.Parameters.AddWithValue("@room", location.Room);
            command.ExecuteNonQuery();
        }
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, -8)]
    public void SensorRegister_NewLocation_SavesEveryField(bool active, int variance)
    {
        SensorRecord created = AddTestSensor(active, variance);
        Assert.True(created.Id > 0);
        Assert.Equal("CoverageMake", created.Make);
        Assert.Equal("CoverageModel", created.Model);
        Assert.Equal(variance, created.Variance);
        Assert.Equal(active, created.Active);
        var location = Assert.Single(SensorsDatabase.GetAllLocations(),
            l => l.Floor == created.Floor && l.Room == created.Room);
        Assert.Equal(active, SensorsDatabase.IsLocationActive(location.Id));
        Assert.Null(SensorsDatabase.GetLatestTemp(location.Id));
    }


    [Fact]
    public void SensorRegister_ExistingLocation_RejectsDuplicateSensor()
    {
        int floor = -2776342;
        int room = 32902;

        // Use an existing sensor's location.
        var sensor = SensorsDatabase.GetAllSensors().First();

        floor = sensor.Floor;
        room = sensor.Room;

        var ex = Assert.Throws<Exception>(() =>
            SensorsDatabase.Register(
                "TestMake",
                "TestModel",
                floor,
                room,
                true,
                0));

        Assert.Contains("already has a sensor", ex.Message);
    }


    [Fact]
    public void UpdateSensor_ExistingSensor_UpdatesOnlyMakeAndModel()
    {
        var sensor = AddTestSensor(false, 7);
        SensorsDatabase.UpdateSensor(sensor.Id, "NewCoverageMake", "NewCoverageModel");
        var updated = Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Id == sensor.Id);
        Assert.Equal("NewCoverageMake", updated.Make);
        Assert.Equal("NewCoverageModel", updated.Model);
        Assert.Equal(sensor.Floor, updated.Floor);
        Assert.Equal(sensor.Room, updated.Room);
        Assert.Equal(7, updated.Variance);
        Assert.False(updated.Active);
    }

    [Fact]
    public void UpdateSensor_UnknownId_DoesNotModifyExistingSensors()
    {
        var sensor = AddTestSensor(true, 4);
        SensorsDatabase.UpdateSensor(-1, "DoesNotExist", "DoesNotExist");
        var unchanged = Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Id == sensor.Id);
        Assert.Equal("CoverageMake", unchanged.Make);
        Assert.Equal("CoverageModel", unchanged.Model);
    }

    [Fact]
    public void UpdateActivity_InitiallyInactiveSensor_TogglesOnThenOff()
    {
        var sensor = AddTestSensor(false, 0);
        int locationId = Assert.Single(SensorsDatabase.GetAllLocations(),
            l => l.Floor == sensor.Floor && l.Room == sensor.Room).Id;
        Assert.False(SensorsDatabase.IsLocationActive(locationId));
        SensorsDatabase.UpdateActivity(true, sensor.Id);
        Assert.True(Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Id == sensor.Id).Active);
        Assert.True(SensorsDatabase.IsLocationActive(locationId));
        SensorsDatabase.UpdateActivity(false, sensor.Id);
        Assert.False(Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Id == sensor.Id).Active);
        Assert.False(SensorsDatabase.IsLocationActive(locationId));
    }

    [Fact]
    public void UpdateActivity_UnknownId_DoesNotChangeKnownSensor()
    {
        var sensor = AddTestSensor(true, 0);
        SensorsDatabase.UpdateActivity(false, -1);
        Assert.True(Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Id == sensor.Id).Active);
    }

    [Fact]
    public void GetAggregates_LocationWithoutReadings_ReturnsEmpty()
    {
        var sensor = AddTestSensor(true, 0);
        int id = Assert.Single(SensorsDatabase.GetAllLocations(),
            l => l.Floor == sensor.Floor && l.Room == sensor.Room).Id;
        Assert.Empty(SensorsDatabase.GetAggregates(new List<int> { id }, Granularity.Daily));
        Assert.Empty(SensorsDatabase.GetChartReadings(id, Granularity.Monthly));
    }

    [Fact]
    public void UserRegister_AdminRole_CanLoginWithAdminRole()
    {
        var (_, username) = AddTestUser("Admin");
        Assert.True(UserDatabase.Login(username, "CoveragePass123!", out string role));
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void UserPasswordUpdate_UnknownId_ReturnsFalse()
    {
        Assert.False(UserDatabase.UpdatePassword(-1, "DoesNotMatter123!"));
    }

    [Fact]
    public void UserRegister_AndUpdate_PreservesCreationDateWhenPassed()
    {
        var (id, username) = AddTestUser();
        var before = Assert.Single(UserDatabase.GetAllUsers(), u => u.Id == id);
        Assert.True(UserDatabase.UpdateUser(id, username, "User", "Updated", "Name", before.date_created));
        var after = Assert.Single(UserDatabase.GetAllUsers(), u => u.Id == id);
        Assert.Equal(before.date_created, after.date_created);
        Assert.Equal("Updated", after.first_name);
        Assert.Equal("Name", after.last_name);
    }

    [Fact]
    public void UserLogin_CaseUnknownUsername_DoesNotAuthenticate()
    {
        var (_, username) = AddTestUser();
        Assert.False(UserDatabase.Login(username + "_missing", "CoveragePass123!", out string role));
        Assert.Equal(string.Empty, role);
    }
}
