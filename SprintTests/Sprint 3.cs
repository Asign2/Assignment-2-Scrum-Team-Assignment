using Assign_2;
using System;
using Xunit;

namespace SprintTests
{
    public class Sprint_3 : IDisposable
    {
        public void Dispose()
        {
            // This automatically runs after EVERY test method finishes.
            // It clears any newly registered users and resets UserDatabase back to default.
            UserDatabase.ResetDatabase();

            // If SensorsDatabase needs a reset or clear as well, you can add it here:
            // SensorsDatabase.ResetDatabase();
        }
        [Fact]
        public void UpdateUser_ValidData_ReturnsTrue()
        {
            // Arrange: Get an existing user (e.g., "alex") to update
            var users = UserDatabase.GetAllUsers();
            var targetUser = users.Find(u => u.Username == "alex");
            Assert.NotNull(targetUser);

            // Act: Update user details
            bool result = UserDatabase.UpdateUser(
                targetUser.Id,
                "alex_updated",
                "User",
                "Alexander",
                "UpdatedLast",
                DateTime.Now);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void UpdateUser_InvalidRole_ThrowsException()
        {
            var users = UserDatabase.GetAllUsers();
            var targetUser = users.Find(u => u.Username == "alex");
            Assert.NotNull(targetUser);

            // Act & Assert: Trying to assign a role that doesn't exist
            var exception = Assert.Throws<Exception>(() =>
            {
                UserDatabase.UpdateUser(
                    targetUser.Id,
                    "alex",
                    "NonExistentRole",
                    "Alex",
                    "User",
                    DateTime.Now);
            });

            Assert.Equal("Invalid role.", exception.Message);
        }

        [Fact]
        public void UpdateUser_DuplicateUsername_ThrowsException()
        {
            var users = UserDatabase.GetAllUsers();
            var targetUser = users.Find(u => u.Username == "alex");
            Assert.NotNull(targetUser);

            // Act & Assert: Try to change Alex's username to "admin", which already exists
            var exception = Assert.Throws<Exception>(() =>
            {
                UserDatabase.UpdateUser(
                    targetUser.Id,
                    "admin", // Already taken by the admin user
                    "User",
                    "Alex",
                    "User",
                    DateTime.Now);
            });

            Assert.Equal("Username already exists.", exception.Message);
        }

        [Fact]
        public void UpdateUser_NonExistentId_ReturnsFalse()
        {
            // Act: Try to update an ID that definitely does not exist
            bool result = UserDatabase.UpdateUser(
                99999,
                "ghostuser",
                "User",
                "Ghost",
                "User",
                DateTime.Now);

            // Assert: Should return false because no rows were affected
            Assert.False(result);
        }

        [Fact]
        public void UpdateUser_ChangesReflectedInDatabase()
        {
            var users = UserDatabase.GetAllUsers();
            var targetUser = users.Find(u => u.Username == "jordan");
            Assert.NotNull(targetUser);

            string newFirst = "JordanUpdated";
            string newLast = "Smith";

            // Act: Update details
            UserDatabase.UpdateUser(
                targetUser.Id,
                "jordan",
                "User",
                newFirst,
                newLast,
                DateTime.Now);

            // Fetch users again to verify the database updated properly
            var updatedUsers = UserDatabase.GetAllUsers();
            var verifiedUser = updatedUsers.Find(u => u.Id == targetUser.Id);

            // Assert
            Assert.NotNull(verifiedUser);
            Assert.Equal(newFirst, verifiedUser.first_name);
            Assert.Equal(newLast, verifiedUser.last_name);
        }

    }
}
