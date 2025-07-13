
using Serilog;



namespace PowerPositionCalculator;

internal class Program
{
    private static async Task Main(string[] args)
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

        // var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
        var logsPathValue = PathUtils.GetRootPath(projectRoot);
        if (logsPathValue.IsError)
            throw new Exception($"Exception loading the path of the logs/base {logsPathValue.Errors.Show()}");

        var timeNow = TimeUtils.GetLondonTime();
        var logDirectory = Path.Combine(logsPathValue.Value, "logs", timeNow.ToString("yyyy-MM-dd"));

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);


        var logFile = Path.Combine(logDirectory, $"logs-{timeNow:HH-mm-ss}.txt");

        Log.Information($"The path of the logs  is {logFile}");

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
        var optsResult = OptionsParser.LoadSettings(args, ct);


        if (optsResult.IsError)
        {
            if (optsResult.Errors is [_, ..] && (optsResult.Errors[0].Code == "help_return"))
            {
                await Log.CloseAndFlushAsync();
                return;
            }

            Log.Fatal("Can't loaded the settings, Error={$Errors}", optsResult.Errors);
            await Log.CloseAndFlushAsync();
            throw new Exception($"Can't loaded the settings {optsResult.Errors.Show()}");
        }

        var opts = optsResult.Value;

        Log.Information("The walker will execute every {TimeMinutes} minutes in folder: {CsvFolderPath}",
            opts.TimeMinutes, opts.CsvFolderPath);

        #endregion

        #endregion


        try
        {
            var minutesTime = opts.TimeMinutes;
            var date = opts.Time;
            var csvPath = opts.CsvFolderPath;
            var retryTimes = opts.RetryTimes;
            var dealyMinutes = opts.DelayMillisecondsInRetryTimes;
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
                            new object[] { date, ct }, retryTimes, ct, dealyMinutes,
                            new[] { typeof(Axpo.PowerServiceException) });

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
                                new object[] { solution.Value, csvPath, now, ct }, retryTimes, ct, dealyMinutes,
                                new[] { typeof(Exception) });

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