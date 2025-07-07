using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace PowerPositionCalculator;

using System;
using System.Net.Http.Headers;
using CommandLine;

internal class Options
{
    [Option('p', "path", Required = true, HelpText = "The CSV output folder path")]
    public required string CsvFolderPath { get; set; }

    [Option('t', "time", Required = true, Default = 25, HelpText = "Time in minutes to the next iteration")]
    public required int TimeMinutes { get; set; }

    [Option('d', "date", Required = false, HelpText = "The date to start the straction")]
    public required DateTime Time { get; set; } = Utils.get_london_time();
}

internal static class Parser
{
    internal static Options LoadSettings(string[] args)
    {
        if (args is [_, ..]) return CommandLine.Parser.Default.ParseArguments<Options>(args).Value;

        // 1. Crear el builder y agregar el archivo JSON
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Directorio base
            .AddJsonFile("appsettings.json", optional: true) // Archivo de configuración
            .Build();


        var section = config.GetSection("options");
        if (section.Exists()) return section.Get<Options>() ?? throw new Exception();

        // Lanzar execption

        throw new Exception();
    }
}