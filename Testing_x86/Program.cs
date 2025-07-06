using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using static Axpo.PowerService; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        DateTime date = new DateTime(2015, 04, 01);
        var solution=await Calculate.Worker(date);

        await CsvGenerator.CrearCsvPowerPositionAsync(solution, @"C:\Users\dell\source\repos\Testing_x86\Testing_x86\datos");
        // Aquí el proceso solo terminará cuando Worker termine
        Console.WriteLine("Proceso finalizado.");
    }
}


