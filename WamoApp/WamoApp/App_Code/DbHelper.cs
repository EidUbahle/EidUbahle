using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WamoApp
{
    public static class DbHelper
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["WamoDb"].ConnectionString;

        public static DataTable ExecuteDataTable(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = commandType;
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public static int ExecuteNonQuery(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandType = commandType;
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandType = commandType;
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public static List<Dictionary<string, object>> ToDictionaryList(DataTable table)
        {
            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in table.Columns) item[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                rows.Add(item);
            }
            return rows;
        }

        public static string SafeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Invalid identifier.");
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '.') throw new InvalidOperationException("Unsafe identifier.");
            }
            return value;
        }
    }
}
