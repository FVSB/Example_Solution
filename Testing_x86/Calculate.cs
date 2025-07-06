using  Axpo; // Cambia esto al namespace real de PowerService.dll
namespace PowerPositionCalculator;

public static class Calculate
{

    public static async void Worker(DateTime date)
    {

        var service = new PowerService();

        IEnumerable<PowerTrade> trades = service.GetTrades(date);

        // Un diccionario para sumar volúmenes por período
        var periodVolumes = new Dictionary<int, double>();

        var asyncArray = new AsyncDoubleArray(24);
        var tasks = new List<Task>();

        foreach (var trade in trades)
        {
            tasks.Add(HiloHandle(asyncArray, trade));
        }


// Esperar a que todas las tareas terminen
        await foreach (var item in asyncArray)
        {
            Console.WriteLine(item);
        }

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