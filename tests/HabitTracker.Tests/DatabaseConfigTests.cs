using HabitTracker.Api.Data;
using Microsoft.Extensions.Configuration;

namespace HabitTracker.Tests;

public class DatabaseConfigTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void ConCadenaCompleta_LaUsaTalCual()
    {
        var config = Config(new() { ["ConnectionStrings:Default"] = "Host=x;Database=y" });
        Assert.Equal("Host=x;Database=y", DatabaseConfig.GetConnectionString(config));
    }

    [Fact]
    public void ConPartes_ArmaLaCadena()
    {
        var config = Config(new()
        {
            ["Database:Host"] = "db.interno",
            ["Database:Name"] = "habits",
            ["Database:User"] = "app",
            ["Database:Password"] = "p;ass=1",
        });

        var result = DatabaseConfig.GetConnectionString(config);

        Assert.Contains("Host=db.interno", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=habits", result);
        Assert.Contains("Username=app", result);
        Assert.Contains("\"p;ass=1\"", result); // la contraseña con caracteres especiales queda escapada
    }

    [Fact]
    public void SinConfiguracion_FallaConMensajeClaro()
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConfig.GetConnectionString(Config([])));
        Assert.Contains("ConnectionStrings__Default", error.Message);
    }
}
