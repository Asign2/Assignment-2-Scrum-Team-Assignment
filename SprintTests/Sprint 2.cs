using Assign_2;
using Xunit;

namespace SprintTests;

public class Sprint_2
{
    [Fact]
    public void Login_SQLInjectionAttempt_ShouldFail()
    {
        bool result = UserDatabase.Login("' OR 1=1 --", "anything", out string role);
        Assert.False(result);
    }

    [Fact]
    public void GetAllUsers_ReturnsUsers()
    {
        var users = UserDatabase.GetAllUsers();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public void GetAllLocations_ReturnsLocations()
    {
        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotNull(locations);
        Assert.NotEmpty(locations);
    }

    [Fact]
    public void GetAllSensors_ReturnsSensors()
    {
        var sensors = SensorsDatabase.GetAllSensors();
        Assert.NotNull(sensors);
        Assert.NotEmpty(sensors);
    }

    [Fact]
    public void GetAllSensors_SensorsHaveLocationInformation()
    {
        var sensors = SensorsDatabase.GetAllSensors();
        Assert.NotEmpty(sensors);
        foreach (var sensor in sensors)
        {
            Assert.False(string.IsNullOrWhiteSpace(sensor.Make));
            Assert.False(string.IsNullOrWhiteSpace(sensor.Model));
            Assert.False(string.IsNullOrWhiteSpace(sensor.Floor.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(sensor.Room.ToString()));
        }
    }
}
