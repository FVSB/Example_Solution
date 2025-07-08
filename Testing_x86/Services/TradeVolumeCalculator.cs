using Axpo; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator;

internal static class TradeVolumeCalculator
{
    internal static async Task<double[]> CalculateTradesVolumenAsync(DateTime date, CancellationToken ct)
    {
        var service = new PowerService();

        ct.ThrowIfCancellationRequested();
        var trades = await service.GetTradesAsync(date);
        ct.ThrowIfCancellationRequested();
        var asyncCalculator = new AsyncTradesVolumeTradesVolumenCalculator(24);

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10,CancellationToken = ct};

        await Parallel.ForEachAsync(trades, options,
            async (trade, ct) => { await AddTradeVolumesAsyncHandle(asyncCalculator, trade, ct); });

        return asyncCalculator.GetArray();
    }

    private static async Task AddTradeVolumesAsyncHandle(AsyncTradesVolumeTradesVolumenCalculator asyncCalculator, PowerTrade trade, CancellationToken ct)
    {
        foreach (var tradePeriods in trade.Periods)
        {
            await asyncCalculator.AddAsync(tradePeriods.Period - 1, tradePeriods.Volume,ct);
        }
    }
}