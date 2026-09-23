using Xunit;
using Assign_2;

namespace SprintTests
{
    public class Sprint_2
    {
        //Code for testing Sql injection (one of the requirements for sprint 2)
        [Fact]
        public void Login_SQLInjectionAttempt_ShouldFail()
        {
            string role;

            bool result = UserDatabase.Login(
                "' OR 1=1 --",
                "anything",
                out role);

            Assert.False(result);
        }
        // Test that all users can be retrieved from the database.
        [Fact]
        public void GetAllUsers_ReturnsUsers()
        {
            var users = UserDatabase.GetAllUsers();

            Assert.NotNull(users);
            Assert.NotEmpty(users);
        }
        //Needs Update, delete, add testing for user


        // Test that locations can be retrieved from the database.
        [Fact]
        public void GetAllLocations_ReturnsLocations()
        {
            var locations = SensorsDatabase.GetAllLocations();

            Assert.NotNull(locations);
            Assert.NotEmpty(locations);
        }
        // Test that sensors can be retrieved from the database.
        [Fact]
        public void GetAllSensors_ReturnsSensors()
        {
            var sensors = SensorsDatabase.GetAllSensors();

            Assert.NotNull(sensors);
            Assert.NotEmpty(sensors);
        }
        // Test that every sensor has location information.
        [Fact]
        public void GetAllSensors_SensorsHaveLocationInformation()
        {
            var sensors = SensorsDatabase.GetAllSensors();

            Assert.NotEmpty(sensors);

            foreach (var sensor in sensors)
            {
                Assert.False(string.IsNullOrWhiteSpace(sensor.Make));
                Assert.False(string.IsNullOrWhiteSpace(sensor.Model));
                Assert.False(string.IsNullOrWhiteSpace(sensor.City));
                Assert.False(string.IsNullOrWhiteSpace(sensor.Suburb));
            }
        }
    }
}
