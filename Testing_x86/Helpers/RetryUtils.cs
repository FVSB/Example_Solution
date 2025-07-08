using ErrorOr;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace PowerPositionCalculator;

public static partial class TimeUtils
{

    /// <summary>
    /// Executes the specified delegate with retry logic for transient errors.
    /// </summary>
    /// <typeparam name="TResult">The return type of the delegate.</typeparam>
    /// <param name="action">The delegate to invoke.</param>
    /// <param name="args">Arguments to pass to the delegate.</param>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="ct">CancellationToken for cooperative cancellation.</param>
    /// <param name="delayMilliseconds">Delay in milliseconds between retries (optional).</param>
    /// <param name="retryOnExceptions">Exception types that trigger a retry.</param>
    /// <returns>An ErrorOr containing the result or a list of errors if all retries fail.</returns>
    public static async Task<ErrorOr<TResult>> RetryAsync<TResult>(
        Delegate action,
        object[] args,
        int maxAttempts,
        CancellationToken ct,
        int delayMilliseconds = 0,
        params Type[] retryOnExceptions)
    {
        var errors = new List<Error>();
        ct.ThrowIfCancellationRequested();

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _logger?.Debug("Attempt {Attempt} of {MaxAttempts} for action {Action}.", attempt, maxAttempts,
                    action.Method.Name);

                // Invoke the delegate using reflection
                var result = action.DynamicInvoke(args);

                if (result is Task<TResult> taskResult)
                {
                    // Await the async result
                    TResult awaitedResult = await taskResult;
                    _logger?.Information("Action {Action} succeeded on attempt {Attempt}.", action.Method.Name,
                        attempt);
                    return awaitedResult;
                }
                else if (result is TResult directResult)
                {
                    // Return direct result if not async
                    _logger?.Information("Action {Action} succeeded on attempt {Attempt}.", action.Method.Name,
                        attempt);
                    return directResult;
                }
                else
                {
                    var error = Error.Failure("InvalidOperationException", "Result is not of the expected type.");
                    errors.Add(error);
                    _logger?.Error("Action {Action} returned an unexpected result type on attempt {Attempt}.",
                        action.Method.Name, attempt);
                    return errors;
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.Warning("Operation was cancelled during attempt {Attempt} of {Action}.", attempt,
                    action.Method.Name);
                throw;
            }
            catch (Exception ex)
            {
                ct.ThrowIfCancellationRequested();

                // Check if the exception is one of the retryable types
                bool canRetry = false;
                foreach (var retryType in retryOnExceptions)
                {
                    if (retryType.IsInstanceOfType(ex) ||
                        (ex.InnerException != null && retryType.IsInstanceOfType(ex.InnerException)))
                    {
                        canRetry = true;
                        break;
                    }
                }

                var error = Error.Unexpected(
                    code: $"RetryAttempt{attempt}",
                    description: $"{ex.GetType().Name}: {ex.Message}");
                errors.Add(error);

                _logger?.Warning(ex, "Attempt {Attempt} failed with {Exception}. Retry eligible: {CanRetry}",
                    attempt, ex.GetType().Name, canRetry);

                if (!canRetry || attempt >= maxAttempts)
                {
                    _logger?.Error("Action {Action} failed after {MaxAttempts} attempts. Returning accumulated errors.",
                        action.Method.Name, maxAttempts);
                    return errors;
                }

                if (delayMilliseconds > 0)
                {
                    _logger?.Debug("Waiting {Delay} ms before next retry of action {Action}.", delayMilliseconds,
                        action.Method.Name);
                    await Task.Delay(delayMilliseconds, ct).ConfigureAwait(false);
                }
            }
        }

        _logger?.Error("Retry logic exhausted for action {Action}. Total errors: {@Errors}", action.Method.Name,
            errors);
        return errors;
    }
}