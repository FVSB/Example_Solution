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

    public static async Task<ErrorOr<TResult>> RetryAsync<TResult>(
        Delegate action,
        object[] args,
        int maxAttempts,
         CancellationToken ct,
        int delayMilliseconds = 0,
        params Type[] retryOnExceptions)
    {
        List<Error> errores = new();

        ct.ThrowIfCancellationRequested();
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Invoca el método usando reflection
                ct.ThrowIfCancellationRequested();
                var result = action.DynamicInvoke(args);

                // Si el método es async, el resultado será un Task<TResult>
                ct.ThrowIfCancellationRequested();
                if (result is Task<TResult> task)
                {
                    TResult resultado = await task;


                    return resultado;
                }
                    return (TResult)result;

            }
            catch (OperationCanceledException ex)
            {
                throw;
            }

            catch (Exception ex)
            {
                ct.ThrowIfCancellationRequested();
                if (retryOnExceptions.Any(t => t.IsInstanceOfType(ex)))
                {
                    errores.Add(Error.Unexpected(
                        code: $"RetryAttempt{attempt}",
                        description: $"{ex.GetType().Name}: {ex.Message}"));
                    if (delayMilliseconds > 0)
                        await Task.Delay(delayMilliseconds,ct);
                }
                else
                {
                    // Si no, lanza la excepción
                    throw;
                }
            }
        }

        // Si se agotaron los intentos, retorna el error
        return errores;
    }
}