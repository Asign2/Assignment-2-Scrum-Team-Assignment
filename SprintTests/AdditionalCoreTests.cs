
using Assign_2;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace SprintUnitTests;

public class AdditionalCoreTests : IDisposable
{
    private readonly List<int> _createdUserIds = new();

    //Locally Creates a test user with a unique username and password. the username is generated using a GUID to ensure uniqueness.
    //The method registers the user in the UserDatabase and returns the user's ID and username for further testing.   
    private (int Id, string Username) CreateTestUser()
    {
        string username = "extra_" + Guid.NewGuid().ToString("N")[..10];

        Assert.True(UserDatabase.Register(
            username, "pass123", "User", "", "", ""));

        UserRecord user = Assert.Single(
            UserDatabase.GetAllUsers(),
            u => u.Username == username);

        _createdUserIds.Add(user.Id);

        return (user.Id, username);
    }

    public void Dispose()
    {
        foreach (int id in _createdUserIds)
        {
            UserDatabase.DeleteUser(id);
        }
    }

    // TEST 1
    [Fact]
    public void Login_EmptyUsername_ReturnsFalse()
    {
        bool result = UserDatabase.Login(
            "", "admin123", out string role);

        Assert.False(result);
        Assert.Equal(string.Empty, role);
    }

    // TEST 2
    [Fact]
    public void Login_EmptyPassword_ReturnsFalse()
    {
        bool result = UserDatabase.Login(
            "admin", "", out string role);

        Assert.False(result);
        Assert.Equal(string.Empty, role);
    }

    // TEST 3
    [Fact]
    public void Login_SQLInjectionInPassword_ReturnsFalse()
    {
        bool result = UserDatabase.Login(
            "admin", "' OR '1'='1", out string role);

        Assert.False(result);
        Assert.Equal(string.Empty, role);
    }

    // TEST 4
    [Fact]
    public void UpdateUser_NegativeId_ReturnsFalse()
    {
        bool result = UserDatabase.UpdateUser(
            -1, "ghost", "User",
            "Test", "Person", DateTime.Now);

        Assert.False(result);
    }

    // TEST 5
    [Fact]
    public void UpdateUser_InvalidRole_DoesNotChangeUser()
    {
        var (id, username) = CreateTestUser();

        Assert.Throws<Exception>(() =>
            UserDatabase.UpdateUser(
                id, username, "InvalidRole",
                "Test", "Person", DateTime.Now));

        UserRecord user = Assert.Single(
            UserDatabase.GetAllUsers(),
            u => u.Id == id);

        Assert.Equal("User", user.Role);
        Assert.Equal(username, user.Username);
    }

    // TEST 6
    [Fact]
    public void UpdateUser_DuplicateUsername_PreservesOriginal()
    {
        var (firstId, firstUsername) = CreateTestUser();
        var (_, secondUsername) = CreateTestUser();

        Assert.Throws<Exception>(() =>
            UserDatabase.UpdateUser(
                firstId, secondUsername, "User",
                "Test", "Person", DateTime.Now));

        UserRecord user = Assert.Single(
            UserDatabase.GetAllUsers(),
            u => u.Id == firstId);

        Assert.Equal(firstUsername, user.Username);
    }

    // TEST 7
    [Fact]
    public void UpdateUser_ChangeToAdmin_UpdatesLoginRole()
    {
        var (id, username) = CreateTestUser();

        Assert.True(UserDatabase.UpdateUser(
            id, username, "Admin",
            "Test", "Person", DateTime.Now));

        Assert.True(UserDatabase.Login(
            username, "pass123", out string role));

        Assert.Equal("Admin", role);
    }

    // TEST 8
    [Fact]
    public void DeleteUser_RemovedAccount_CannotLogin()
    {
        var (id, username) = CreateTestUser();

        Assert.True(UserDatabase.DeleteUser(id));

        bool result = UserDatabase.Login(
            username, "pass123", out string role);

        Assert.False(result);
        Assert.DoesNotContain(
            UserDatabase.GetAllUsers(),
            u => u.Id == id);
    }

    // TEST 9
    [Fact]
    public void UpdatePassword_OldPasswordStopsWorking()
    {
        var (id, username) = CreateTestUser();

        Assert.True(UserDatabase.UpdatePassword(
            id, "NewPassword456"));

        Assert.False(UserDatabase.Login(
            username, "pass123", out _));

        Assert.True(UserDatabase.Login(
            username, "NewPassword456", out string role));

        Assert.Equal("User", role);
    }

    [Fact]
    public void Query_ReturnsColumnNamesAndValues()
    {
        var results = JsonExport.Query(
            "SELECT 1 AS Id, 'Test' AS Name");

        Assert.Single(results);
        Assert.Equal(1, results[0]["Id"]);
        Assert.Equal("Test", results[0]["Name"]);
    }

    [Fact]
    public void SaveJsonTest_ExportDataToJson()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.json");
        try
        {
            var data = new { Id = 1, Name = "Test" };

            JsonExport.WriteJson(filePath, data);

            Assert.True(File.Exists(filePath));

            string json = File.ReadAllText(filePath);
            Assert.Contains("\"Id\": 1", json);
            Assert.Contains("\"Name\": \"Test\"", json);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
