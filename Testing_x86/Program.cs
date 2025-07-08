using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using CommandLine;
using static Axpo.PowerService; // Cambia esto al namespace real de PowerService.dll
using System.Reflection;
using ErrorOr;
using LanguageExt;

namespace PowerPositionCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Console.WriteLine("Cancelling...");
            cts.Cancel();
            eventArgs.Cancel = true;
        };
        try
        {
            var opts = Parser.LoadSettings(args,ct);


            Console.WriteLine($"The walker execute every {opts.TimeMinutes} to the folder {opts.CsvFolderPath}");
            var minutesTime = opts.TimeMinutes;
            var date = opts.Time;
            var csvPath = opts.CsvFolderPath;

            do
            {
                _ = Task.Run(async Task<LanguageExt.Unit> () =>
                {
                    ct.ThrowIfCancellationRequested();

                    var now = Utils.get_london_time();
                    date = date.AddMinutes(minutesTime);

                    ct.ThrowIfCancellationRequested();
                    var solution = await Utils.RetryAsync<double[]>(Calculate.CalculateTradesVolumenAsync,
                        new object[] { date,ct }, 10, ct,2000,
                        new Type[]{
                        typeof(Axpo.PowerServiceException)
                    });

                    ct.ThrowIfCancellationRequested();
                    if (solution.IsError)
                    {
                        //TODO: Make the error MSG
                        Console.WriteLine("Error");
                    }


                    ct.ThrowIfCancellationRequested();
                    await Utils.RetryAsync<LanguageExt.Unit>(CsvGenerator.CrearCsvPowerPositionAsync,
                        new object[] { solution.Value, csvPath, now, ct }, 10, ct,2000,new Type[]{typeof(Exception)});

                    ct.ThrowIfCancellationRequested();
                    System.Console.WriteLine($"Executed the walker in time {now}");
                    return LanguageExt.Unit.Default;
                },ct);



                await Task.Delay(TimeSpan.FromMinutes(minutesTime),ct);
            } while (true);
        }
        catch (OperationCanceledException)
        {

            Console.WriteLine("Operation canceled by the user.");
        }

    }
}