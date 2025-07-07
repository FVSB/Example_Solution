using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using CommandLine;
using static Axpo.PowerService; // Cambia esto al namespace real de PowerService.dll
using System.Reflection;

namespace PowerPositionCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        var opts = Parser.LoadSettings(args);


        Console.WriteLine($"The walker execute every {opts.TimeMinutes} to the folder {opts.CsvFolderPath}");
        var minutesTime = opts.TimeMinutes;
        var date = opts.Time;
        var csvPath = opts.CsvFolderPath;

        do
        {
            var now = Utils.get_london_time();
            date = date.AddMinutes(minutesTime);
            var solution = await Utils.RetryAsync<DateTime, double[]>(Calculate.Worker, date, 10, 2000,
                typeof(Axpo.PowerServiceException));
            //var solution=await Calculate.Worker(date);
            if (solution.IsError)
            {
                Console.WriteLine("No se logro");
                return;
            }

            await CsvGenerator.CrearCsvPowerPositionAsync(solution.Value, csvPath, now);

            System.Console.WriteLine($"Executed the walker in time {now}");

            await Task.Delay(TimeSpan.FromMinutes(minutesTime));

            Console.WriteLine($"Ëxecuted the walker in time {now}");
        } while (true);
    }
}