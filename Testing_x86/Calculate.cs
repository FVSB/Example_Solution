using  Axpo; // Cambia esto al namespace real de PowerService.dll
namespace PowerPositionCalculator;

public static class Calculate
{

    public static async Task<double[]> Worker(DateTime date)
    {

        var service = new PowerService();

        var trades = await  service.GetTradesAsync(date);

        var asyncArray = new AsyncDoubleArray(24);

        var tasks = new List<Task>();

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
        await Parallel.ForEachAsync(trades, options, async (trade, ct) =>
        {
            await HiloHandle(asyncArray, trade);
        });





// Esperar a que todas las tareas terminen
        int a=1;
        await foreach (var item in asyncArray)
        {
            Console.WriteLine($"Item:{item}, index:{a++}");

        }

        return asyncArray.GetArray();

    }

    public static async Task HiloHandle(AsyncDoubleArray asyncArray, PowerTrade trade )
    {
        foreach (var tradePeriods in trade.Periods)
        {
            if (tradePeriods.Period == 1) Console.WriteLine($"Los periodos valen {tradePeriods.Volume}");
           await asyncArray.AddAsync(tradePeriods.Period - 1, tradePeriods.Volume);
        }
    }
}