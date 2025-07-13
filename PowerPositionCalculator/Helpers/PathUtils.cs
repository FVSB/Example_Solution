using Serilog;
using ErrorOr;
namespace PowerPositionCalculator;

public static  class PathUtils
{
    private static readonly ILogger Logger = Log.ForContext(typeof(PathUtils));/// <summary>
    /// Checks if the last directory of the given path is "bin".
    /// If so, moves two more levels up. Returns the new path or an error using ErrorOr.
    /// Never throws exceptions.
    /// </summary>
    /// <param name="basePath">The starting path to check and possibly adjust.</param>
    /// <returns>
    /// An ErrorOr<string> containing the adjusted root path,
    /// or an error if the operation cannot be completed.
    /// </returns>
    internal static ErrorOr<string> GetRootPath(string basePath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(basePath);

            Logger.Information("Checking if the last directory is 'bin' for path: {BasePath}", basePath);

            if (dirInfo.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Information("'bin' directory detected. Moving two levels up.");

                dirInfo = dirInfo.Parent;
                if (dirInfo == null)
                {
                    Logger.Warning("Unable to move two levels up from 'bin' directory.");
                    return ErrorOr.Error.Failure("RootPath.NotFound", "Could not move two levels up from 'bin'.");
                }

                Logger.Information("New root path: {RootPath}", dirInfo.FullName);
                return dirInfo.FullName;
            }

            Logger.Information("No adjustment needed. Returning original path.");
            return basePath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error while getting root path.");
            return ErrorOr.Error.Unexpected(description: ex.Message);
        }
    }

}