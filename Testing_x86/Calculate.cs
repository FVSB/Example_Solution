using Axpo; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator;

internal static class Calculate
{
    internal static async Task<double[]> CalculateTradesVolumenAsync(DateTime date, CancellationToken ct)
    {
        var service = new PowerService();

        ct.ThrowIfCancellationRequested();
        var trades = await service.GetTradesAsync(date);
        ct.ThrowIfCancellationRequested();
        var asyncCalculator = new AsyncTradesVolumenCalculator(24);

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10,CancellationToken = ct};

        await Parallel.ForEachAsync(trades, options,
            async (trade, ct) => { await AddTradeVolumesAsyncHandle(asyncCalculator, trade, ct); });

        return asyncCalculator.GetArray();
    }

    private static async Task AddTradeVolumesAsyncHandle(AsyncTradesVolumenCalculator asyncCalculator, PowerTrade trade, CancellationToken ct)
    {
        foreach (var tradePeriods in trade.Periods)
        {
            await asyncCalculator.AddAsync(tradePeriods.Period - 1, tradePeriods.Volume,ct);
        }
    }
}