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
        Func<object[], Task<ErrorOr<TResult>>> action,
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
            {
                // Invoca el método usando reflection
                ct.ThrowIfCancellationRequested();
                var result = action.DynamicInvoke(args);
                if (result is null)
                {
                    errors.Add(Error.Unexpected("null", $"Can't be null, must be Task<ErrOr<{typeof(TResult)}>>"));
                    return errors;
                }

                // Si el método es async, el resultado será un Task<TResult>
                ct.ThrowIfCancellationRequested();
                if (result is not Task<ErrorOr<TResult>>)
                {
                    return Error.Unexpected(description: $"Unexpected {result.GetType()} ");
                }

                var task = (Task<ErrorOr<TResult>>)result;

                var rErrorOr = await task;

                if (!rErrorOr.IsError) return rErrorOr.Value;

                if (!(rErrorOr.Errors.Any(e => retryOnExceptions.Any(t => t.IsInstanceOfType(e)))))
                {
                    return Error.Failure("Fail", $"Fail must of {maxAttempts}");
                }

                ct.ThrowIfCancellationRequested();
                errors.Add(Error.Unexpected(
                    code: $"RetryAttempt{attempt}",
                    description:
                    $"{string.Join(", ", rErrorOr.Errors.Select(e => e.GetType().Name))}: {string.Join(", ", rErrorOr.Errors.Select(e => e.Description))}"));
                if (delayMilliseconds > 0)
                    await Task.Delay(delayMilliseconds, ct);
            }
            catch (Exception e)
            {
                errors.Add(Error.Unexpected($"Exception running RetryAsync {e.GetType()}".ToString(),e.Message));
                return errors;
            }
        }


        return errors;
    }
}