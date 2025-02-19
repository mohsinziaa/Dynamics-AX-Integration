using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ax.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Method to get a database connection
        private async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        // Generic method to execute a query and return a list of results
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, Func<SqlDataReader, T> mapFunction, Dictionary<string, object>? parameters = null)
        {
            var results = new List<T>();

            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand(sql, connection);

                // Add parameters if provided
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(mapFunction(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database query failed: {ex.Message}");
            }

            return results;
        }

        // Method to execute a query that doesn't return any data (for INSERT, UPDATE, DELETE)
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            int rowsAffected = 0;

            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand(sql, connection);

                // Add parameters if provided
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                rowsAffected = await command.ExecuteNonQueryAsync(); // Get affected rows
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database command failed: {ex.Message}");
            }

            return rowsAffected; // Return affected rows count
        }

    }
}
