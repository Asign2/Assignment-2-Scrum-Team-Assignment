using System;
using System.Collections.Generic;
using Assign_2;
using Xunit;

namespace SprintTests;

public class Sprint_3 : IDisposable
{
    private readonly List<int> _testUserIds = new();

    // Clean up only the rows created by THIS test, never reset the shared DB.
    public void Dispose()
    {
        foreach (int id in _testUserIds)
            UserDatabase.DeleteUser(id);
    }
    //Locally Creates a test user with a unique username and password. the username is generated using a GUID to ensure uniqueness.
    //The method registers the user in the UserDatabase and returns the user's ID and username for further testing.   
    private (int Id, string Username) CreateTestUser()
    {
        string username = "s3_" + Guid.NewGuid().ToString("N")[..10];
        Assert.True(UserDatabase.Register(username, "pass123", "User", "", "", ""));
        UserRecord record = Assert.Single(UserDatabase.GetAllUsers(),
            u => u.Username == username);
        _testUserIds.Add(record.Id);
        return (record.Id, username);
    }

    [Fact]
    public void UpdateUser_ValidData_ReturnsTrue()
    {
        var (id, _) = CreateTestUser();
        string changedUsername = "s3_updated_" + Guid.NewGuid().ToString("N")[..10];

        bool result = UserDatabase.UpdateUser(id, changedUsername, "User",
            "Alexander", "UpdatedLast", DateTime.Now);

        Assert.True(result);
        UserRecord updated = Assert.Single(UserDatabase.GetAllUsers(), u => u.Id == id);
        Assert.Equal(changedUsername, updated.Username);
    }

    [Fact]
    public void UpdateUser_InvalidRole_ThrowsException()
    {
        var (id, username) = CreateTestUser();
        var exception = Assert.Throws<Exception>(() =>
            UserDatabase.UpdateUser(id, username, "NonExistentRole",
                "Test", "User", DateTime.Now));

        Assert.Equal("Invalid role.", exception.Message);
    }

    [Fact]
    public void UpdateUser_DuplicateUsername_ThrowsException()
    {
        var (firstId, _) = CreateTestUser();
        var (_, alreadyTakenUsername) = CreateTestUser();

        var exception = Assert.Throws<Exception>(() =>
            UserDatabase.UpdateUser(firstId, alreadyTakenUsername, "User",
                "Test", "User", DateTime.Now));

        Assert.Equal("Username already exists.", exception.Message);
    }

    [Fact]
    public void UpdateUser_NonExistentId_ReturnsFalse()
    {
        bool result = UserDatabase.UpdateUser(int.MaxValue,
            "ghostuser_" + Guid.NewGuid().ToString("N")[..10],
            "User", "Ghost", "User", DateTime.Now);

        Assert.False(result);
    }

    [Fact]
    public void UpdateUser_ChangesReflectedInDatabase()
    {
        var (id, username) = CreateTestUser();
        const string newFirst = "JordanUpdated";
        const string newLast = "Smith";

        bool result = UserDatabase.UpdateUser(id, username, "User",
            newFirst, newLast, DateTime.Now);
        Assert.True(result);

        UserRecord verified = Assert.Single(UserDatabase.GetAllUsers(), u => u.Id == id);
        Assert.Equal(newFirst, verified.first_name);
        Assert.Equal(newLast, verified.last_name);
    }
}
