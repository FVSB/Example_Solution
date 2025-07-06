using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using static Axpo.PowerService; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime date = DateTime.Today; // o puedes pedir al usuario la fecha
            Console.WriteLine($"Calculando posiciones para: {date:yyyy-MM-dd}");

            var service = new PowerService();

            IEnumerable<PowerTrade> trades = service.GetTrades(date);

            // Un diccionario para sumar volúmenes por período
            var periodVolumes = new Dictionary<int, double>();

            foreach (var trade in trades)
            {
                foreach (var period in trade.Periods)
                {
                    Console.WriteLine(period);
                    Console.WriteLine(period.Period);
                    Console.WriteLine(period.Volume);
                    if (!periodVolumes.ContainsKey(period.Period))
                        periodVolumes[period.Period] = 0;

                    periodVolumes[period.Period] += period.Volume;
                }
            }

            // Ordenar por periodo
            var sortedPeriods = periodVolumes.OrderBy(p => p.Key);

            // Guardar en CSV
            string fileName = $"PowerPosition_{date:yyyyMMdd}.csv";
            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine("Local Time,Volume");
                foreach (var p in sortedPeriods)
                {
                    DateTime localTime = date.AddHours(p.Key - 1);
                    writer.WriteLine($"{localTime:HH:mm},{p.Value}");
                }
            }

            Console.WriteLine($"Posiciones guardadas en {fileName}");
        }
    }
}

