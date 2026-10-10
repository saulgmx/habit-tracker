using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HabitTracker.Api.Data;

/// <summary>
/// Cadena de conexión a PostgreSQL. Se toma de <c>ConnectionStrings:Default</c> o, si no existe,
/// se arma con <c>Database:Host</c>, <c>Port</c>, <c>Name</c>, <c>User</c> y <c>Password</c>
/// (variables de entorno <c>Database__Host</c>, etc.), para que la contraseña llegue como secreto aparte.
/// </summary>
public static class DatabaseConfig
{
    public static string GetConnectionString(IConfiguration config)
    {
        var full = config.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(full)) return full;

        var section = config.GetSection("Database");
        var host = section["Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException(
                "Falta la conexión a PostgreSQL: define ConnectionStrings__Default o Database__Host, Database__Name, Database__User y Database__Password.");

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = section.GetValue("Port", 5432),
            Database = section["Name"] ?? "habits",
            Username = section["User"],
            Password = section["Password"],
        }.ConnectionString;
    }
}
