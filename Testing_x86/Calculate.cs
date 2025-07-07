using Axpo; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator;

public static class Calculate
{
    public static async Task<double[]> Worker(DateTime date)
    {
        var service = new PowerService();

        var trades = await service.GetTradesAsync(date);

        var asyncArray = new AsyncDoubleArray(24);

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
        await Parallel.ForEachAsync(trades, options, async (trade, ct) => { await HiloHandle(asyncArray, trade); });


        return asyncArray.GetArray();
    }

    public static async Task HiloHandle(AsyncDoubleArray asyncArray, PowerTrade trade)
    {
        foreach (var tradePeriods in trade.Periods)
        {
            await asyncArray.AddAsync(tradePeriods.Period - 1, tradePeriods.Volume);
        }
    }


}