using System;
using System.Collections.Generic;
using System.Linq;
using Assign_2;
using Microsoft.Data.SqlClient;
using Xunit;

namespace SprintUnitTests;

public class SprintFeatureTests : IDisposable
{
    private readonly List<int> _createdUserIds = new();
    private readonly List<string> _createdLogMarkers = new();
    private DashboardSettings? _originalSettings;

    // Removes only records created by these tests.
    //Also Ensures that any settings changed during the tests are revertedd to their original state.
    //this is done to avoid issues or wrong behavior in future tests or application use.

    public void Dispose()
    {
        foreach (int userId in _createdUserIds)
            UserDatabase.DeleteUser(userId);

        foreach (string marker in _createdLogMarkers)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(
                "DELETE FROM dbo.DashboardLog WHERE ViewedBy = @marker;", connection);
            command.Parameters.AddWithValue("@marker", marker);
            command.ExecuteNonQuery();
        }

        if (_originalSettings != null)
        {
            // Restore the settings that were present before this test.
            SensorsDatabase.SaveSettings(_originalSettings, _originalSettings.UpdatedBy);
            SensorsDatabase.UpdateSettings(_originalSettings);
        }
    }

    private (int Id, string Username) CreateTestUser()
    {
        string username = "sprint_" + Guid.NewGuid().ToString("N")[..10];
        Assert.True(UserDatabase.Register(username, "pass123", "User", "", "", ""));
        UserRecord user = Assert.Single(UserDatabase.GetAllUsers(),
            u => u.Username == username);
        _createdUserIds.Add(user.Id);
        return (user.Id, username);
    }

    private static List<LocationRecord> GetSeededLocations()
    {
        SensorsDatabase.Initialize();
        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotEmpty(locations);
        return locations;
    }

    [Fact]
    public void Login_ValidAdminCredentials_ReturnsAdminRole()
    {
        bool success = UserDatabase.Login("admin", "admin123", out string role);
        Assert.True(success);
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void Login_ValidUserCredentials_ReturnsUserRole()
    {
        var (_, username) = CreateTestUser();
        bool success = UserDatabase.Login(username, "pass123", out string role);
        Assert.True(success);
        Assert.Equal("User", role);
    }

    [Fact]
    public void Login_InvalidPassword_ReturnsFalseAndEmptyRole()
    {
        bool success = UserDatabase.Login("admin", "incorrect", out string role);
        Assert.False(success);
        Assert.Equal(string.Empty, role);
    }

    [Fact]
    public void Login_UnknownUsername_ReturnsFalse()
    {
        bool success = UserDatabase.Login("missing_" + Guid.NewGuid().ToString("N"),
            "pass123", out string role);
        Assert.False(success);
        Assert.Equal(string.Empty, role);
    }

    [Fact]
    public void Register_NewUser_CanLogIn()
    {
        var (_, username) = CreateTestUser();
        Assert.True(UserDatabase.Login(username, "pass123", out string role));
        Assert.Equal("User", role);
    }

    [Fact]
    public void Register_DuplicateUsername_ThrowsException()
    {
        Assert.Throws<Exception>(() =>
            UserDatabase.Register("admin", "pass123", "User", "", "", ""));
    }

    [Fact]
    public void Register_InvalidRole_ThrowsException()
    {
        Assert.Throws<Exception>(() =>
            UserDatabase.Register("invalid_" + Guid.NewGuid().ToString("N"),
                "pass123", "NoSuchRole", "", "", ""));
    }

    [Fact]
    public void GetAllUsers_AfterRegister_ContainsNewUser()
    {
        var (_, username) = CreateTestUser();
        Assert.Contains(UserDatabase.GetAllUsers(), u => u.Username == username);
    }

    [Fact]
    public void UpdateUser_ValidUser_UpdatesDetailsAndRole()
    {
        var (id, _) = CreateTestUser();
        string newUsername = "changed_" + Guid.NewGuid().ToString("N")[..10];

        bool updated = UserDatabase.UpdateUser(
            id, newUsername, "Admin", "Test", "Person", DateTime.Now);

        Assert.True(updated);
        UserRecord record = Assert.Single(UserDatabase.GetAllUsers(), u => u.Id == id);
        Assert.Equal(newUsername, record.Username);
        Assert.Equal("Admin", record.Role);
        Assert.Equal("Test", record.first_name);
        Assert.Equal("Person", record.last_name);
        Assert.True(UserDatabase.Login(newUsername, "pass123", out string role));
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void UpdatePassword_ValidUser_ChangesLoginPassword()
    {
        var (id, username) = CreateTestUser();
        Assert.True(UserDatabase.UpdatePassword(id, "newPass456"));
        Assert.False(UserDatabase.Login(username, "pass123", out _));
        Assert.True(UserDatabase.Login(username, "newPass456", out _));
    }

    [Fact]
    public void DeleteUser_ExistingUser_RemovesUser()
    {
        var (id, username) = CreateTestUser();
        Assert.True(UserDatabase.DeleteUser(id));
        Assert.False(UserDatabase.Login(username, "pass123", out _));
        Assert.DoesNotContain(UserDatabase.GetAllUsers(), u => u.Id == id);
    }

    [Fact]
    public void DeleteUser_UnknownId_ReturnsFalse()
    {
        Assert.False(UserDatabase.DeleteUser(-1));
    }

    [Fact]
    public void Initialize_CreatesLocationsAndSensors()
    {
        var locations = GetSeededLocations();
        var sensors = SensorsDatabase.GetAllSensors();
        Assert.NotEmpty(locations);
        Assert.NotEmpty(sensors);
        Assert.All(sensors, s => Assert.True(s.Id > 0));
    }

    [Fact]
    public void GetAggregates_ValidLocation_ReturnsTemperatureSummary()
    {
        int locationId = GetSeededLocations()[0].Id;
        var result = SensorsDatabase.GetAggregates(
            new List<int> { locationId }, Granularity.Daily);

        Assert.NotEmpty(result);
        Assert.All(result, r =>
        {
            Assert.True(r.MinTemp <= r.AvgTemp);
            Assert.True(r.AvgTemp <= r.MaxTemp);
            Assert.True(r.Samples > 0);
        });
    }

    [Fact]
    public void GetAggregates_EmptyLocations_ReturnsEmptyList()
    {
        var result = SensorsDatabase.GetAggregates(new List<int>(), Granularity.Monthly);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    [InlineData(Granularity.Yearly)]
    public void GetChartReadings_ValidGranularity_ReturnsReadings(Granularity granularity)
    {
        int locationId = GetSeededLocations()[0].Id;
        var readings = SensorsDatabase.GetChartReadings(locationId, granularity);
        Assert.NotEmpty(readings);
        Assert.All(readings, r => Assert.False(double.IsNaN(r.Temperature)));
    }

    [Fact]
    public void GetChartReadings_InvalidGranularity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SensorsDatabase.GetChartReadings(1, (Granularity)999));
    }

    [Fact]
    public void GetLatestTemp_ValidLocation_ReturnsTemperature()
    {
        int locationId = GetSeededLocations()[0].Id;
        double? temperature = SensorsDatabase.GetLatestTemp(locationId);
        Assert.NotNull(temperature);
        Assert.True(double.IsFinite(temperature.Value));
    }

    [Fact]
    public void GetLatestTemp_InvalidLocation_ReturnsNull()
    {
        SensorsDatabase.Initialize();
        Assert.Null(SensorsDatabase.GetLatestTemp(-1));
    }

    [Fact]
    public void GetSettings_InitializedDatabase_ReturnsSettings()
    {
        SensorsDatabase.Initialize();
        DashboardSettings settings = SensorsDatabase.GetSettings();
        Assert.False(string.IsNullOrWhiteSpace(settings.DefaultGranularity));
        Assert.True(settings.Tick >= 0);
    }

    [Fact]
    public void SaveSettings_NewValues_AreSavedCorrectly()
    {
        SensorsDatabase.Initialize();
        _originalSettings = SensorsDatabase.GetSettings();
        string granularity = _originalSettings.DefaultGranularity == "Weekly"
            ? "Monthly" : "Weekly";
        var changed = new DashboardSettings
        {
            DefaultGranularity = granularity,
            ShowCurrentTemp = !_originalSettings.ShowCurrentTemp,
            ShowTempComparison = !_originalSettings.ShowTempComparison,
            ShowTempReading = !_originalSettings.ShowTempReading,
            Tick = _originalSettings.Tick
        };

        SensorsDatabase.SaveSettings(changed, "SprintUnitTests");
        DashboardSettings saved = SensorsDatabase.GetSettings();

        Assert.Equal(granularity, saved.DefaultGranularity);
        Assert.Equal(changed.ShowCurrentTemp, saved.ShowCurrentTemp);
        Assert.Equal(changed.ShowTempComparison, saved.ShowTempComparison);
        Assert.Equal(changed.ShowTempReading, saved.ShowTempReading);
        Assert.Equal("SprintUnitTests", saved.UpdatedBy);
    }

    [Fact]
    public void UpdateSettings_NewTick_IsSavedCorrectly()
    {
        SensorsDatabase.Initialize();
        _originalSettings = SensorsDatabase.GetSettings();
        var changed = new DashboardSettings { Tick = _originalSettings.Tick + 1 };

        SensorsDatabase.UpdateSettings(changed);

        Assert.Equal(changed.Tick, SensorsDatabase.GetSettings().Tick);
    }

    [Fact]
    public void RecordDashboard_NewSnapshot_AppearsInLog()
    {
        SensorsDatabase.Initialize();
        string marker = "SprintTest_" + Guid.NewGuid().ToString("N")[..10];
        _createdLogMarkers.Add(marker);
        var snapshot = new DashboardSnapshot
        {
            ViewedBy = marker,
            Location = "Floor 1, Room 1",
            Granularity = "Monthly",
            AvgTemp = 21.45
        };

        SensorsDatabase.RecordDashboard(snapshot);
        var logs = SensorsDatabase.GetDashboardLog(10000);
        Assert.Contains(logs, l =>
            l.ViewedBy == marker &&
            l.Location == snapshot.Location &&
            l.Granularity == snapshot.Granularity &&
            Math.Abs(l.AvgTemp - snapshot.AvgTemp) < 0.001);
    }

    [Fact]
    public void GetDashboardLog_LimitOfThree_ReturnsAtMostThreeRecords()
    {
        SensorsDatabase.Initialize();
        var logs = SensorsDatabase.GetDashboardLog(3);
        Assert.InRange(logs.Count, 0, 3);
    }
}
