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
    List<Error> errors = new();
    ct.ThrowIfCancellationRequested();
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {

        try
        { ct.ThrowIfCancellationRequested();
            // Invoca el método usando reflection
            var result = action.DynamicInvoke(args);

            // Si el método es async, el resultado será un Task<TResult>
            if (result is Task<TResult> task)
            {
                TResult resultado = await task;
                return resultado;
            }
            else if (result is TResult directResult)
            {
                // Si no es async, simplemente retorna el resultado
                return directResult;
            }
            else
            {
                errors.Add(Error.Failure("InvalidOperationException", "The result is not of the expected type."));
                return errors;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ct.ThrowIfCancellationRequested();
            //TODO: TO the logger

            bool canRetry = false;
            foreach (var type in retryOnExceptions)
            {
                // Considera también InnerException (por DynamicInvoke)
                if (type.IsInstanceOfType(ex) ||
                    (ex.InnerException != null && type.IsInstanceOfType(ex.InnerException)))
                {

                    canRetry = true;
                    break;
                }
            }
            ct.ThrowIfCancellationRequested();
            errors.Add(Error.Unexpected(
                code: $"RetryAttempt{attempt}",
                description: $"{ex.GetType().Name}: {ex.Message}"));

            ct.ThrowIfCancellationRequested();
            if (!(canRetry && attempt < maxAttempts))
            {
                return errors;
                continue; // Reintenta
            }
            if (delayMilliseconds > 0)
                await Task.Delay(delayMilliseconds,ct);
        }
    }

    return errors;
}

}