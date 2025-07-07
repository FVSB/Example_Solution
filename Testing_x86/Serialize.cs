using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using System.Collections.Generic;

namespace PowerPositionCalculator;


public class PowerPositionRecord
{
    public string LocalTime { get; set; }
    public double Volume { get; set; }
}

public static class CsvGenerator
{
    private static readonly object _fileLock = new object();

    public static async Task CrearCsvPowerPositionAsync(double[] volumes, string folderPath,DateTime dateTime)
    {
        if (volumes == null || volumes.Length != 24)
            throw new ArgumentException("El array debe tener exactamente 24 elementos.");


        string fileName = $"PowerPosition_{dateTime:yyyyMMdd}_{dateTime:HHmm}.csv";
        string fullPath = Path.Combine(folderPath, fileName);

        var records = new List<PowerPositionRecord>();
        for (int i = 0; i < 24; i++)
        {
            int hour = (23 + i) % 24;
            string hora = hour.ToString("D2") + ":00";
            records.Add(new PowerPositionRecord
            {
                LocalTime = hora,
                Volume = volumes[i]
            });
        }

        // Solo un hilo puede entrar aquí a la vez
        lock (_fileLock)
        {
            using (var writer = new StreamWriter(fullPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
        }

        // El await aquí es solo para mantener la firma asíncrona
        await Task.CompletedTask;
    }
}