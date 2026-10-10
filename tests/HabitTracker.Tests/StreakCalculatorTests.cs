using HabitTracker.Api.Domain;

namespace HabitTracker.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static DateOnly Ago(int days) => Today.AddDays(-days);

    [Fact]
    public void Current_SinCheckins_EsCero() =>
        Assert.Equal(0, StreakCalculator.Current([], Today));

    [Fact]
    public void Current_SoloHoy_EsUno() =>
        Assert.Equal(1, StreakCalculator.Current([Today], Today));

    [Fact]
    public void Current_HoyAyerYAnteayer_EsTres() =>
        Assert.Equal(3, StreakCalculator.Current([Today, Ago(1), Ago(2)], Today));

    [Fact]
    public void Current_AyerYAnteayerSinHoy_SigueViva() =>
        Assert.Equal(2, StreakCalculator.Current([Ago(1), Ago(2)], Today));

    [Fact]
    public void Current_HoyYHaceDosDias_FaltaAyer_EsUno() =>
        Assert.Equal(1, StreakCalculator.Current([Today, Ago(2)], Today));

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Current_UltimoCheckinHaceDosDiasOMas_EsCero(int days) =>
        Assert.Equal(0, StreakCalculator.Current([Ago(days), Ago(days + 1)], Today));

    [Fact]
    public void Current_CruceDeMes_CuentaContinua()
    {
        var today = new DateOnly(2026, 2, 1);
        DateOnly[] dates = [new(2026, 1, 30), new(2026, 1, 31), today];
        Assert.Equal(3, StreakCalculator.Current(dates, today));
    }

    [Fact]
    public void Current_CruceDeAnio_CuentaContinua()
    {
        var today = new DateOnly(2027, 1, 1);
        DateOnly[] dates = [new(2026, 12, 30), new(2026, 12, 31), today];
        Assert.Equal(3, StreakCalculator.Current(dates, today));
    }

    [Fact]
    public void Current_AnioBisiesto_CuentaContinua()
    {
        var today = new DateOnly(2028, 3, 1);
        DateOnly[] dates = [new(2028, 2, 28), new(2028, 2, 29), today];
        Assert.Equal(3, StreakCalculator.Current(dates, today));
    }

    [Fact]
    public void Current_FechasDuplicadas_NoSuman() =>
        Assert.Equal(2, StreakCalculator.Current([Today, Today, Ago(1), Ago(1)], Today));

    [Fact]
    public void Current_FechasDesordenadas_MismoResultado()
    {
        DateOnly[] ordered = [Ago(3), Ago(2), Ago(1), Today];
        DateOnly[] shuffled = [Ago(1), Today, Ago(3), Ago(2)];
        Assert.Equal(StreakCalculator.Current(ordered, Today), StreakCalculator.Current(shuffled, Today));
        Assert.Equal(4, StreakCalculator.Current(shuffled, Today));
    }

    [Fact]
    public void Best_ConVariasRachasHistoricas_DevuelveLaMayor()
    {
        DateOnly[] dates =
        [
            Ago(30), Ago(29),                  // racha de 2
            Ago(20), Ago(19), Ago(18), Ago(17), // racha de 4
            Ago(10), Ago(9), Ago(8),            // racha de 3
        ];
        Assert.Equal(4, StreakCalculator.Best(dates));
    }

    [Fact]
    public void Best_SinCheckins_EsCero() =>
        Assert.Equal(0, StreakCalculator.Best([]));

    [Fact]
    public void Best_DuplicadasYDesordenadas_Correcto() =>
        Assert.Equal(3, StreakCalculator.Best([Ago(1), Today, Ago(1), Ago(2), Ago(10)]));
}
