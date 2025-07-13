using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using System.Collections.Generic;
using Serilog;

namespace PowerPositionCalculator
{
    public record PowerPositionRecord
    {
        /// <summary>
        /// Local time in "HH:mm" format representing the hour of the record.
        /// </summary>
        public string LocalTime { get; set; }

        /// <summary>
        /// Volume value for the given time slot.
        /// </summary>
        public double Volume { get; set; }
    }

    public static class CsvGenerator
    {
        // Semaphore to ensure exclusive file write access.
        private static readonly SemaphoreSlim FileSemaphore = new SemaphoreSlim(1, 1);

        // Logger specific for this class.
        private static readonly ILogger Logger = Log.ForContext(typeof(CsvGenerator));

        /// <summary>
        /// Asynchronously creates a CSV file containing 24 hourly volume records for a specific date and time.
        /// </summary>
        /// <param name="volumes">Array of 24 double values representing hourly volumes.</param>
        /// <param name="folderPath">Directory where the CSV file will be saved.</param>
        /// <param name="dateTime">DateTime used to generate the filename and context.</param>
        /// <param name="ct">Cancellation token to cancel the operation.</param>
        /// <returns>A LanguageExt.Unit indicating completion.</returns>
        /// <exception cref="ArgumentException">Thrown if volumes array is null or does not have exactly 24 elements.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<LanguageExt.Unit> CreatePowerPositionCsvAsync(
            double[] volumes,
            string folderPath,
            DateTime dateTime,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (volumes == null || volumes.Length != 24)
            {
                var lengthInfo = volumes is not null ? volumes.Length.ToString() : "null";
                var message = $"The volumes array must contain exactly 24 elements, but has {lengthInfo}.";
                Logger.Error("CSV creation error: {ErrorMessage} with input volumes length: {VolumesLength}", message, lengthInfo);
                throw new ArgumentException(message);
            }

            string fileName = $"PowerPosition_{dateTime:yyyyMMdd}_{dateTime:HHmm}.csv";
            string fullPath = Path.Combine(folderPath, fileName);
            Logger.Information("Starting CSV file creation at {FilePath}", fullPath);

            var records = new List<PowerPositionRecord>(24);
            for (int i = 0; i < 24; i++)
            {
                int hour = (23 + i) % 24; // Adjust hour per original logic
                string formattedHour = hour.ToString("D2") + ":00";

                records.Add(new PowerPositionRecord
                {
                    LocalTime = formattedHour,
                    Volume = volumes[i]
                });
            }

            try
            {
                await FileSemaphore.WaitAsync(ct);
                try
                {
                    await using var writer = new StreamWriter(fullPath);
                    await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    await csv.WriteRecordsAsync(records, ct);
                }
                finally
                {
                    FileSemaphore.Release();
                }

                Logger.Information("CSV file successfully created at {FilePath}", fullPath);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.Error(ex, "Failed to write CSV file at {FilePath}", fullPath);
                throw;
            }

            return LanguageExt.Unit.Default;
        }
    }
}
