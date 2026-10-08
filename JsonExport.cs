using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace Assign_2
{
    internal class JsonExport
    {
        public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

        /// Asks where to save, then writes the data as indented JSON. Does nothing if cancelled.
        public static void SaveJson(string fileName, object data)
        {
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = fileName };
            if (dialog.ShowDialog() == true)
                File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(data, Pretty));
        }

        /// Runs a SELECT and returns each row as a column-name and adds the  value dictionary. Helps bypass a bug i had.
        public static List<Dictionary<string, object>> Query(string sql)
        {
            using var connection = new SqlConnection(UserDatabase.ConnectionString);
            connection.Open();
            using var reader = new SqlCommand(sql, connection).ExecuteReader();

            var rows = new List<Dictionary<string, object>>();
            while (reader.Read())
                rows.Add(Enumerable.Range(0, reader.FieldCount)
                    .ToDictionary(i => reader.GetName(i), i => reader.IsDBNull(i) ? null : reader.GetValue(i)));
            return rows;
        }
    }
}
