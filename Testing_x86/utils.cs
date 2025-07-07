namespace PowerPositionCalculator;
using ErrorOr;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class Utils
{
    public static DateTime get_london_time()
    {
        // Obtén el objeto de zona horaria para Londres
        TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

        // Obtén la hora UTC actual
        DateTime utcNow = DateTime.UtcNow;

        // Convierte la hora UTC a hora de Londres
        DateTime londonNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, londonTimeZone);

        return londonNow;
    }


    public static async Task<ErrorOr<TResult>> RetryAsync<TInput, TResult>(
        Func<TInput, Task<TResult>> action,
        TInput input,
        int maxAttempts,
        int delayMilliseconds = 0,
        params Type[] retryOnExceptions)
    {
        List<Error> errores = new();

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                TResult resultado = await action(input);
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hubo {attempt} errores");
                bool esReintentable = false;
                foreach (var tipo in retryOnExceptions)
                {
                    if (tipo.IsInstanceOfType(ex))
                    {
                        esReintentable = true;
                        break;
                    }
                }

                errores.Add(Error.Unexpected(
                    code: $"RetryAttempt{attempt}",
                    description: $"{ex.GetType().Name}: {ex.Message}"));

                if (esReintentable && attempt < maxAttempts)
                {
                    if (delayMilliseconds > 0)
                        await Task.Delay(delayMilliseconds);
                    continue; // Reintenta
                }
                else
                {
                    // No es una excepción reintentable o es el último intento
                    return errores;
                }
            }
        }

        // Si todos los intentos fallan con excepciones reintentables
        return errores;
    }



}

