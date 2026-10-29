using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;

namespace Assign_2
{
    /// <summary>
    /// Handles user accounts and database connections.
    /// </summary>
    public static class UserDatabase
    {
        private static string ProjectRoot =
            Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\"));



        private static readonly bool IsTestDatabase =
            Environment.GetEnvironmentVariable("ASSIGN2_TEST_DB") == "1";

        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Assign_2",
            IsTestDatabase ? "testdatabase.mdf" : "appdatabase.mdf");

        private static readonly string DatabaseName =
            IsTestDatabase ? "UserDb_Test" : "UserDb_Root";



        internal static string ConnectionString =
            $@"Server=(localdb)\mssqllocaldb;
                AttachDbFilename={DbPath};
                Database={DatabaseName};
                Integrated Security=True;
                TrustServerCertificate=True;";

        private const string MasterConnectionString =
            @"Server=(localdb)\mssqllocaldb;
              Database=master;
              Integrated Security=True;
              TrustServerCertificate=True;";

        // =========================================================
        // DATABASE STARTUP
        // =========================================================

        static UserDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            using var master =
                new SqlConnection(MasterConnectionString);

            master.Open();

            bool isRegistered = RunScalarBool(
                master,
                $"SELECT COUNT(*) FROM sys.databases WHERE name = '{DatabaseName}';");

            bool fileExists = File.Exists(DbPath);

            // Database exists but MDF file is missing
            if (isRegistered && !fileExists)
            {
                RunNonQuery(
                    master,
                    $"DROP DATABASE IF EXISTS [{DatabaseName}];");

                isRegistered = false;
            }

            // MDF exists but database isn't attached
            if (!isRegistered && fileExists)
            {
                RunNonQuery(
                    master,
                    $"CREATE DATABASE [{DatabaseName}] " +
                    $"ON (FILENAME = '{DbPath}') FOR ATTACH;");
            }
            // MDF doesn't exist
            else if (!isRegistered && !fileExists)
            {
                RunNonQuery(
                    master,
                    $"CREATE DATABASE [{DatabaseName}] " +
                    $"ON PRIMARY " +
                    $"(NAME = {DatabaseName}_Data, " +
                    $"FILENAME = '{DbPath}');");
            }

            using var connection = OpenConnection();

            // =====================================================
            // ROLES TABLE
            // =====================================================

            bool rolesExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Roles', 'U');",
                true);

            if (!rolesExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Roles
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        RoleName NVARCHAR(50) UNIQUE NOT NULL
                    );");

                SeedRoles(connection);
            }

            // =====================================================
            // USERS TABLE
            // =====================================================

            bool usersExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Users', 'U');",
                true);

            if (!usersExists)
            {
                CreateUsersTable(connection);
                SeedDefaultUsers(connection);
            }
            SensorsDatabase.Initialize();
        }

        // =========================================================
        // CREATE USERS TABLE
        // =========================================================

        private static void CreateUsersTable(
            SqlConnection connection)
        {
            RunNonQuery(connection, @"
                CREATE TABLE dbo.Users
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,

                    Username NVARCHAR(255) UNIQUE NOT NULL,

                    PasswordHash NVARCHAR(MAX) NOT NULL,

                    firstnames NVARCHAR(255) NOT NULL,

                    lastnames NVARCHAR(255) NOT NULL,

                    date_created DATETIME2 NOT NULL,

                    RoleId INT NOT NULL,

                    CONSTRAINT FK_Users_Roles
                        FOREIGN KEY (RoleId)
                        REFERENCES dbo.Roles(Id)
                );");
        }

        // =========================================================
        // SEED ROLES
        // =========================================================

        private static void SeedRoles(
            SqlConnection connection)
        {
            string[] roles =
            {
                "Admin",
                "User"
            };

            foreach (string role in roles)
            {
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Roles
                        (RoleName)
                      VALUES
                        (@role);",
                    connection);

                command.Parameters.AddWithValue(
                    "@role",
                    role);

                command.ExecuteNonQuery();
            }
        }

        // =========================================================
        // SEED DEFAULT USERS
        // =========================================================
        
        private static void SeedDefaultUsers(SqlConnection connection)
        {
            // Define the tuple with explicit names so user.Username, user.Password, etc., work
            var defaultUsers = new (string Username, string Password, int RoleId, string Firstname, string Lastname)[]
            {
                ("admin", "admin123", 1, "Admin", "Account"),
                ("alex", "user123", 2, "Alex", "User"),
                ("jordan", "user123", 2, "Jordan", "User"),
                ("sam", "user123", 2, "Sam", "User"),
                ("taylor", "user123", 2, "Taylor", "User"),
                ("tiffany", "tiffmeister2000", 2, "Tiffany", "Masters"),
                ("backup", "backup123", 1, "Backupcharacter1", "")
            };

            foreach (var user in defaultUsers)
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(user.Password);

                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Users
                    (
                        Username,
                        PasswordHash,
                        firstnames,
                        lastnames,
                        date_created,
                        RoleId
                    )
                    VALUES
                    (
                        @username,
                        @hash,
                        @firstname,
                        @lastname,
                        @date_created,
                        @roleId
                    );",
                    connection);

                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@hash", hash);
                command.Parameters.AddWithValue("@firstname", user.Firstname);
                command.Parameters.AddWithValue("@lastname", user.Lastname);
                command.Parameters.AddWithValue("@date_created", DateTime.Now); // Automatically uses current time
                command.Parameters.AddWithValue("@roleId", user.RoleId);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Checks login details and returns the user role if valid.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static bool Login(
            string username,
            string password,
            out string role)
        {
            role = string.Empty;

            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"SELECT
                        r.RoleName,
                        u.PasswordHash
                      FROM dbo.Users u
                      INNER JOIN dbo.Roles r
                        ON u.RoleId = r.Id
                      WHERE u.Username = @username;",
                    connection);

            command.Parameters.AddWithValue(
                "@username",
                username);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return false;
            }

            string roleName =
                reader.GetString(0);

            string storedHash =
                reader.GetString(1);

            bool passwordCorrect =
                BCrypt.Net.BCrypt.Verify(
                    password,
                    storedHash);

            if (!passwordCorrect)
            {
                return false;
            }

            role = roleName;

            return true;
        }

        /// <summary>
        /// Registers a new user account with the selected role.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="role"></param>
        /// <param name="firstname"></param>
        /// <param name="lastname"></param>
        /// <param name="datecreated"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool Register(
            string username,
            string password,
            string role,
            string firstname,
            string lastname,
            string datecreated
            )
        {
            using var connection =
                OpenConnection();

            // Get RoleId
            using var roleCommand =
                new SqlCommand(
                    @"SELECT Id
                      FROM dbo.Roles
                      WHERE RoleName = @role;",
                    connection);

            roleCommand.Parameters.AddWithValue(
                "@role",
                role);

            object roleResult =
                roleCommand.ExecuteScalar();

            if (roleResult == null)
            {
                throw new Exception(
                    "Invalid role.");
            }

            int roleId =
                Convert.ToInt32(roleResult);

            using var command =
                new SqlCommand(
                    @"INSERT INTO dbo.Users
                    (
                        Username,
                        PasswordHash,
                        firstnames,
                        lastnames,
                        date_created,
                        RoleId
                    )
                    VALUES
                    (
                        @username,
                        @hash,
                        @firstname,
                        @lastname,
                        @date_created,
                        @roleId
                    );",
                    connection);

            command.Parameters.AddWithValue(
                "@username",
                username);

            command.Parameters.AddWithValue(
                "@hash",
                BCrypt.Net.BCrypt.HashPassword(
                    password));

            command.Parameters.AddWithValue(
                "@firstname",
                firstname);

            command.Parameters.AddWithValue(
                "@lastname",
                lastname);

            command.Parameters.AddWithValue(
                "@date_created",
                DateTime.Now);

            command.Parameters.AddWithValue(
                "@roleId",
                roleId);

            try
            {
                command.ExecuteNonQuery();

                return true;
            }


            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new Exception("Username already exists.", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception(
                    "A database error occurred while saving the user.", ex);
            }


        }

        /// <summary>
        /// Gets all users and their account information.
        /// </summary>
        /// <returns></returns>
        public static List<UserRecord> GetAllUsers()
        {
            var users = new List<UserRecord>();

            using var connection = OpenConnection();

            using var command = new SqlCommand(
                @"SELECT
                    u.Id,
                    u.Username,
                    r.RoleName,
                    u.firstnames,
                    u.lastnames,
                    u.date_created
                FROM dbo.Users u
                INNER JOIN dbo.Roles r
                    ON u.RoleId = r.Id
                ORDER BY u.Id;",
                connection);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(
                    new UserRecord
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Role = reader.GetString(2),
                        first_name = reader.GetString(3),
                        last_name = reader.GetString(4),
                        date_created = reader.GetDateTime(5)
                    });
            }

            return users;
        }

        /// <summary>
        /// Updates an existing user account.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newUsername"></param>
        /// <param name="newRole"></param>
        /// <param name="newFirstName"></param>
        /// <param name="newLastName"></param>
        /// <param name="newDateCreated"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool UpdateUser(
            int id,
            string newUsername,
            string newRole,
            string newFirstName,
            string newLastName,
            DateTime newDateCreated)  
        {
            using var connection =
                OpenConnection();

            // Find RoleId
            using var roleCommand =
                new SqlCommand(
                    @"SELECT Id
                      FROM dbo.Roles
                      WHERE RoleName = @role;",
                    connection);

            roleCommand.Parameters.AddWithValue(
                "@role",
                newRole);

            object roleResult =
                roleCommand.ExecuteScalar();

            if (roleResult == null)
            {
                throw new Exception(
                    "Invalid role.");
            }

            int roleId =
                Convert.ToInt32(roleResult);

            using var command =
                new SqlCommand(
                    @"UPDATE dbo.Users
                      SET Username = @username,
                          RoleId = @roleId,
                          firstnames = @firstname,
                          lastnames = @lastname,
                          date_created = @dateCreated
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue("@username", newUsername);
            command.Parameters.AddWithValue("@roleId", roleId);
            command.Parameters.AddWithValue("@firstname", newFirstName);
            command.Parameters.AddWithValue("@lastname", newLastName);
            command.Parameters.AddWithValue("@dateCreated", newDateCreated);
            command.Parameters.AddWithValue("@id", id);

            try
            {
                return command.ExecuteNonQuery() > 0;
            }

            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new Exception("Username already exists.", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception(
                    "A database error occurred while saving the user.", ex);
            }


        }

        /// <summary>
        /// Changes the password for an existing user.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public static bool UpdatePassword(
            int id,
            string newPassword)
        {
            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"UPDATE dbo.Users
                      SET PasswordHash = @hash
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue(
                "@hash",
                BCrypt.Net.BCrypt.HashPassword(
                    newPassword));

            command.Parameters.AddWithValue(
                "@id",
                id);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Deletes a user account by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool DeleteUser(
            int id)
        {
            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"DELETE FROM dbo.Users
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue(
                "@id",
                id);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Opens and returns a connection to the application database.
        /// </summary>
        /// <returns></returns>
        public static SqlConnection OpenConnection()
        {
            var connection =
                new SqlConnection(
                    ConnectionString);

            connection.Open();

            return connection;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private static void RunNonQuery(
            SqlConnection connection,
            string sql)
        {
            using var command =
                new SqlCommand(
                    sql,
                    connection);

            command.ExecuteNonQuery();
        }

        private static bool RunScalarBool(
            SqlConnection connection,
            string sql,
            bool checkNotNull = false)
        {
            using var command =
                new SqlCommand(
                    sql,
                    connection);

            object result =
                command.ExecuteScalar();

            if (checkNotNull)
            {
                return result != null &&
                       result != DBNull.Value;
            }

            return Convert.ToInt32(result) > 0;
        }
        /// <summary>
        /// Deletes all users and restores the default accounts. Use only for testing.
        /// </summary>

        /// <summary>
        /// Resets user accounts in a dedicated test database.
        /// </summary>
        public static void ResetDatabase()
        {
            // Never reset the normal application database.
            if (!string.Equals(
                new SqlConnectionStringBuilder(ConnectionString).InitialCatalog,
                "UserDb_Test",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Database reset is only allowed for UserDb_Test.");
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var deleteCommand = new SqlCommand(
                    "DELETE FROM dbo.Users;",
                    connection,
                    transaction);

                deleteCommand.ExecuteNonQuery();

                using var resetCommand = new SqlCommand(
                    "DBCC CHECKIDENT ('dbo.Users', RESEED, 0);",
                    connection,
                    transaction);

                resetCommand.ExecuteNonQuery();

                // SeedDefaultUsers currently accepts no transaction,
                // so do not call it until it is updated.
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

    }
}