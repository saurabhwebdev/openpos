using Npgsql;
using Dapper;

namespace MyWinFormsApp.Helpers;

public static class DatabaseHelper
{
    static DatabaseHelper()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public static NpgsqlConnection GetConnection()
        => new(AppConfig.GetConnectionString());

    public static async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            var result = await connection.ExecuteScalarAsync<string>("SELECT version();");
            return (true, $"Connected successfully!\n{result}");
        }
        catch (NpgsqlException ex)
        {
            return (false, $"Database connection failed:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error:\n{ex.Message}");
        }
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        using var connection = GetConnection();
        return await connection.QueryAsync<T>(sql, parameters);
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        using var connection = GetConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public static async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using var connection = GetConnection();
        return await connection.ExecuteAsync(sql, parameters);
    }

    public static async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }
}
