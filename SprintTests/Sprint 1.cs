using System;
using System.Collections.Generic;
using Assign_2;
using Xunit;

namespace SprintUnitTests;

public class UserDatabaseTests : IDisposable
{
    private readonly List<int> _testUserIds = new();

    // Delete ONLY the test accounts created by this test instance.
    public void Dispose()
    {
        foreach (int id in _testUserIds)
            UserDatabase.DeleteUser(id);
    }
    //Locally Creates a test user with a unique username and password. the username is generated using a GUID to ensure uniqueness.
    //The method registers the user in the UserDatabase and returns the user's ID and username for further testing.    
    private (int Id, string Username) CreateTestUser(string password = "user123")
    {
        string username = "s1_" + Guid.NewGuid().ToString("N")[..10];
        Assert.True(UserDatabase.Register(username, password, "User", "", "", ""));

        UserRecord record = Assert.Single(UserDatabase.GetAllUsers(),
            u => u.Username == username);
        _testUserIds.Add(record.Id);
        return (record.Id, username);
    }

    [Fact]
    public void Login_ValidAdminCredentials_ReturnsTrueAndAdminRole()
    {
        bool success = UserDatabase.Login("admin", "admin123", out string role);
        Assert.True(success);
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void Login_ValidUserCredentials_ReturnsTrueAndUserRole()
    {
        var (_, username) = CreateTestUser();
        bool success = UserDatabase.Login(username, "user123", out string role);
        Assert.True(success);
        Assert.Equal("User", role);
    }

    [Fact]
    public void Login_InvalidPassword_ReturnsFalse()
    {
        bool success = UserDatabase.Login("admin", "wrongpassword", out string role);
        Assert.False(success);
    }

    [Fact]
    public void Login_NonExistentUser_ReturnsFalse()
    {
        string username = "missing_" + Guid.NewGuid().ToString("N");
        bool success = UserDatabase.Login(username, "password", out string role);
        Assert.False(success);
    }

    [Fact]
    public void Register_NewUser_SuccessfullyAddsUser()
    {
        var (_, username) = CreateTestUser("pass123");
        bool loginSuccess = UserDatabase.Login(username, "pass123", out string role);
        Assert.True(loginSuccess);
        Assert.Equal("User", role);
    }

    [Fact]
    public void Register_DuplicateUsername_ThrowsException()
    {
        var (_, username) = CreateTestUser();
        Assert.Throws<Exception>(() =>
            UserDatabase.Register(username, "somepassword", "User", "", "", ""));
    }
}
