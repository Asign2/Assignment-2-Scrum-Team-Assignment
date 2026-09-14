using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    /// <summary>
    /// The Columns that are in the Users table of the database are represented by this class.
    /// </summary>
    public class UserRecord
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string first_name { get; set; }

        public string last_name { get; set; }
        
        public string password_hash { get; set; }
        
        public DateTime date_created  { get; set; }
    }
}