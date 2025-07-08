using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using ErrorOr;
using Serilog;

namespace PowerPositionCalculator;

using System;
using System.Net.Http.Headers;
using CommandLine;

/// <summary>
/// Represents the command-line options or configuration settings required for the application.
/// </summary>
internal class Options
{
    /// <summary>
    /// Gets or sets the folder path where CSV files will be saved.
    /// </summary>
    [Option('p', "path", Required = true, HelpText = "The folder path where CSV files will be saved.")]
    public required string CsvFolderPath { get; set; }

    /// <summary>
    /// Gets or sets the time interval in minutes for the next iteration.
    /// </summary>
    [Option('t', "time", Required = true, Default = 25, HelpText = "Time interval in minutes for the next iteration.")]
    public required int TimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the date and time to start the extraction process. Defaults to current London time.
    /// </summary>
    [Option('d', "date", Required = false, HelpText = "Optional date and time to start the extraction.")]
    public required DateTime Time { get; set; } = TimeUtils.GetLondonTime();
}

/// <summary>
/// Provides functionality to load application settings either from command-line arguments or from a configuration file.
/// </summary>
internal static class OptionsParser
{
    private static readonly ILogger _logger = Log.ForContext(typeof(OptionsParser));

    /// <summary>
    /// Loads the application settings from command-line arguments or appsettings.json as a fallback.
    /// </summary>
    /// <param name="args">The command-line arguments provided to the application.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> result containing the loaded <see cref="Options"/> or an error.</returns>
    internal static ErrorOr<Options> LoadSettings(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Starting to load application settings...");
            cancellationToken.ThrowIfCancellationRequested();

            if (args is [_, ..]) return CommandLine.Parser.Default.ParseArguments<Options>(args).Value;

            cancellationToken.ThrowIfCancellationRequested();

            // Fallback to appsettings.json
            _logger.Information("Falling back to appsettings.json configuration.");

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Directorio base
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false) // Archivo de configuración
                .Build();

            cancellationToken.ThrowIfCancellationRequested();

            var section = config.GetSection("options");
            if (section.Exists())
            {
                var options = section.Get<Options>();
                if (options != null)
                {
                    _logger.Information("Settings successfully loaded from appsettings.json.");
                    return options;
                }

            }

            _logger.Error("Configuration section 'options' not found or invalid in appsettings.json.");
            return ErrorOr.Error.Unexpected(
                code: "LoadSettingsError",
                description: "Failed to retrieve options from configuration file."
            );

            return ErrorOr.Error.Unexpected("Unexpedted in Load Settings", "Can't get the options");
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Settings loading was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error occurred while loading settings.");
            return ErrorOr.Error.Failure(
                code: ex.Source ?? "LoadSettingsException",
                description: ex.Message
            );
        }

    }
}