using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using CommandLine;
using static Axpo.PowerService;
using System.Reflection;
using ErrorOr;
using LanguageExt;
using Microsoft.VisualBasic.CompilerServices;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using ErrorType = ErrorOr.ErrorType;


namespace PowerPositionCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        #region Configurations

        #region Cancellation Token Config

        var cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Log.Warning("Cancellation requested by user (Ctrl+C).");
            cts.Cancel();
            eventArgs.Cancel = true;
        };

        #endregion

        #region Logger Config

        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
        var logDirectory = Path.Combine(projectRoot, "logs");

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);


        var logFile = Path.Combine(logDirectory, $"logs_{TimeUtils.GetLondonTime().Minute}_{TimeUtils.GetLondonTime().Second}.txt");
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(logFile, buffered: false)
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();


        #endregion

        #region Load Config Options

        Log.Information("Starting PowerPositionCalculator application.");
        // Load the options from appsettings.json or console
        var opts_result = OptionsParser.LoadSettings(args, ct);



        if (opts_result.IsError)
        {
            if (opts_result.Errors is [_, ..] && (opts_result.Errors[0].Code == "help_return"))
            {
               await Log.CloseAndFlushAsync();
               return;
            }
            Log.Fatal("Can't loaded the settings, Error={$Errors}", opts_result.Errors);
            await Log.CloseAndFlushAsync();
            throw new Exception($"Can't loaded the settings {opts_result.Errors.Show()}");
        }

        var opts = opts_result.Value;

       // Log.Debug("Loaded settings: TimeMinutes={TimeMinutes}, CsvFolderPath={CsvFolderPath}, Time={Time}",
        //    opts.TimeMinutes, opts.CsvFolderPath, opts.Time);

        Log.Information("The walker will execute every {TimeMinutes} minutes in folder: {CsvFolderPath}",
            opts.TimeMinutes, opts.CsvFolderPath);

        #endregion


        #endregion


        try
        {
            var minutesTime = opts.TimeMinutes;
            var date = opts.Time;
            var csvPath = opts.CsvFolderPath;

            do
            {
                _ = Task.Run(async Task<LanguageExt.Unit>? () =>
                {
                    ct.ThrowIfCancellationRequested();

                    var now = TimeUtils.GetLondonTime();
                    date = date.AddMinutes(minutesTime);

                    Log.Information("Starting trade volume calculation at {Now} for target time {TargetTime}", now,
                        date);

                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        var solution = await RetryUtils.RetryAsync<double[]>(
                            TradeVolumeCalculator.CalculateTradesVolumenAsync,
                            new object[] { date, ct }, 10, ct, 2000,
                            new Type[] { typeof(Axpo.PowerServiceException) });

                        if (solution.IsError)
                        {
                            Log.Error("Trade volume calculation failed at {Now}. Error details: {@Errors}",
                                now, solution.Errors);
                        }
                        else
                        {
                            Log.Information("Trade volume calculation completed successfully at {Now}", now);

                            ct.ThrowIfCancellationRequested();
                            await RetryUtils.RetryAsync<LanguageExt.Unit>(CsvGenerator.CreatePowerPositionCsvAsync,
                                new object[] { solution.Value, csvPath, now, ct }, 10, ct, 2000,
                                new Type[] { typeof(Exception) });

                            Log.Information("CSV generated successfully at {CsvPath} for time {Now}", csvPath, now);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "An unexpected error occurred during the walker execution.");
                        throw;
                    }

                    Log.Debug("Walker cycle completed at {Now}", now);
                    return LanguageExt.Unit.Default;
                }, ct);


                await Task.Delay(TimeSpan.FromMinutes(minutesTime), ct);
            } while (true);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Operation canceled by user.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in PowerPositionCalculator.");
        }
        finally
        {
            Log.Information("PowerPositionCalculator shutting down.");
            await Log.CloseAndFlushAsync();
        }

        return;
    }
}