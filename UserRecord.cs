
using System;

namespace Assign_2
{
    /// <summary>
    /// Represents a user account stored in the database.
    /// </summary>
    public class UserRecord
    {
        /// <summary>
        /// The unique ID of the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The username used to log in.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The user's account role.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// The user's first name.
        /// </summary>
        public string first_name { get; set; }

        /// <summary>
        /// The user's last name.
        /// </summary>
        public string last_name { get; set; }

        /// <summary>
        /// The hashed password stored for the user.
        /// </summary>
        public string password_hash { get; set; }

        /// <summary>
        /// The date and time the account was created.
        /// </summary>
        public DateTime date_created { get; set; }
    }
}
