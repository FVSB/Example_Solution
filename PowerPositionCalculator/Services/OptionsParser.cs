using System.Reflection;
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


    /// <summary>
    /// Overrides properties in the Options class using command-line arguments.
    /// </summary>
    /// <param name="args">An array of command-line arguments.</param>
    /// <returns>
    /// Returns a LanguageExt.Unit wrapped in an ErrorOr if successful,
    /// otherwise returns an ErrorOr.Error.Failure.
    /// </returns>
    internal ErrorOr<LanguageExt.Unit> OverrideFromArgs(string[] args, ILogger logger)
    {
        try
        {
            // Tipos que necesitan nueva refefinicion
            var specialTypes = new Dictionary<string, Func<string, ErrorOr<object>>>()
            {
                {
                    nameof(Options.Time), (string dateString) =>
                    {
                        logger.Debug("Parsing special type 'Time' with value: {Value}", dateString);
                        // Trim seconds if present and parse datetime
                        dateString = dateString.Substring(0, 16);

                        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm", null,
                                System.Globalization.DateTimeStyles.None,
                                out DateTime reformatDate)) ;
                        {
                            logger.Debug("Successfully parsed datetime: {ParsedDate}", reformatDate);
                            return reformatDate;
                        }
                        logger.Warning("Invalid datetime format for value: {Value}", dateString);
                        return ErrorOr.Error.Validation($"Invalid datetime format for value '{dateString}'");
                    }
                }
            };

            var type = typeof(Options);
            var props = type.GetProperties();
            foreach (var prop in type.GetProperties())
            {
                var name = prop.Name;
                var optionAttr = prop.GetCustomAttribute<OptionAttribute>();
                if (optionAttr is null)
                {
                    logger.Warning("Missing OptionAttribute for property: {PropertyName}", name);
                    ErrorOr.Error.Failure($"Missing OptionAttribute for property {name}");
                }

                // Search for the argument using short or long name
                var indexOpt = Array.IndexOf(args, $"-{optionAttr?.ShortName}");
                if (indexOpt < 0)
                {
                    indexOpt = Array.IndexOf(args, $"--{optionAttr?.LongName}");

                    if (indexOpt < 0) continue;
                }

                if (indexOpt + 1 >= args.Length)
                {
                    return ErrorOr.Error.Failure(
                        "Invalid argument",
                        $"The argument {args[indexOpt]} does not have a value assigned."
                    );
                }


                var value = args[indexOpt + 1];
                logger.Debug("Found argument {Arg} with value: {Value}", args[indexOpt], value);

                var typeName = prop.PropertyType.FullName ?? "ErrorOr.Error";

                Type destinationType = Type.GetType(typeName) ?? typeof(ErrorOr.Error);

                if (destinationType == typeof(ErrorOr.Error))
                {
                    logger.Error("Property {PropertyName} type resolution failed.", name);
                    return ErrorOr.Error.Validation("Null Error", $"The property {name} does not exist");
                }

                PropertyInfo? propInfo = typeof(Options).GetProperty(name);

                if (propInfo is null)
                {
                    logger.Error("Property {PropertyName} does not exist on Options.", name);
                    ErrorOr.Error.Failure($"The property {name} does not exist", $"The property {name} does not exist");
                }

                // Check for special types needing custom parsing
                if (specialTypes.ContainsKey(name))
                {
                    var temp = specialTypes[name](value);
                    if (temp.IsError)
                    {
                        logger.Fatal("Failed to parse special type for property {PropertyName}.", name);
                        return temp.Errors;
                    }

                    logger.Information("Setting special type property {PropertyName} to {Value}", name, temp.Value);
                    propInfo?.SetValue(this, temp);
                    continue;
                }

                // Convert and set the property value

                var convertedValue = Convert.ChangeType(value, destinationType);
                logger.Information("Setting property {PropertyName} to {Value}", name, convertedValue);

                propInfo?.SetValue(this, convertedValue);
            }

            logger.Information("All applicable settings successfully overridden from console arguments.");
            return LanguageExt.Unit.Default;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error occurred during OverrideFromArgs.");
            return ErrorOr.Error.Failure(
                "OverrideFromArgsException",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Logs all properties of the current <see cref="Options"/> instance that have the <see cref="OptionAttribute"/> attribute,
    /// using the provided <paramref name="logger"/>.
    /// </summary>
    /// <param name="logger">The logger instance used to write information and error messages.</param>
    /// <returns>
    /// Returns <see cref="LanguageExt.Unit.Default"/> on success, or an error result of type <see cref="ErrorOr{LanguageExt.Unit}"/>
    /// if an exception occurs during the logging process.
    /// </returns>
    /// <remarks>
    /// Each property decorated with <see cref="OptionAttribute"/> will have its name and current value logged as information.
    /// In case of an exception, an error message is logged and an error result is returned.
    /// </remarks>
    internal ErrorOr<LanguageExt.Unit> Show(ILogger logger)
    {
        try
        {
            var type = typeof(Options);
            var temp = "Loaded settings:";
            foreach (var prop in type.GetProperties())
            {
                var optionAttr = prop.GetCustomAttribute<OptionAttribute>();
                if (optionAttr != null)
                {
                    var name = prop.Name;
                    temp += $" {name}={type.GetProperty(name).GetValue(this)} ";
                }
            }

            logger.Information(temp);
            return LanguageExt.Unit.Default;
        }
        catch (Exception e)
        {
            logger.Error(e, "An error occurred while showing options.");
            return ErrorOr.Error.Failure("An error occurred while showing options.", e.Message);
        }
    }
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


            if (args is [_, ..] && (args.Contains("--help") || args.Contains("-h")))
            {
                if (args.Length > 1)
                {
                    return ErrorOr.Error.Validation(
                        "Fatal error: Help option must be used alone without other arguments.");
                }

                var parser = new Parser(with => with.HelpWriter = Console.Out);
                parser.ParseArguments<Options>(new[] { "--help" });
                return ErrorOr.Error.Custom(6,"help_return","");
            }


            // Fallback to appsettings.json
            _logger.Information("Falling back to appsettings.json configuration.");

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            cancellationToken.ThrowIfCancellationRequested();

            var section = config.GetSection("options");
            if (!section.Exists())
            {
                return ErrorOr.Error.Unexpected(
                    code: "LoadSettingsError",
                    description: "Failed to retrieve options from configuration file."
                );
            }

            var options = section.Get<Options>();
            if (options is not null)
            {
                _logger.Information("Settings successfully loaded from appsettings.json.");

                cancellationToken.ThrowIfCancellationRequested();

                if (args is [_, ..])
                {
                    options.OverrideFromArgs(args, _logger);
                    _logger.Information("Settings successfully overridden from console args");
                }

                // Show the options in the logs
                options.Show(_logger);
                return options;
            }


            _logger.Error("Configuration section 'options' not found or invalid in appsettings.json.");


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