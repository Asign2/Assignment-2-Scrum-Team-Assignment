using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace Assign_2
{
    /// <summary>
    ///  Provides External Utlity Methods for the internal Json Methods inside mainwindow.cs and querys databases into dynamic dictionarys
    /// </summary>
    public class JsonExport
    {
        public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

        /// <summary>
        /// Asks where to save, then writes the data as indented JSON. Does nothing if cancelled.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public static void SaveJson(string fileName, object data)
        {
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = fileName };
            if (dialog.ShowDialog() == true)
                WriteJson(dialog.FileName, data);
        }

        /// <summary>
        /// Runs a SELECT and returns each row as a column-name and adds the  value dictionary. Helps bypass a bug i had.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
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
        /// <summary>
        ///Writes all the Text from the name and data to the file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public static void WriteJson(string fileName, object data) 
        {
            File.WriteAllText(fileName, JsonSerializer.Serialize(data, Pretty));
        }
    }
}
