using ErrorOr;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace PowerPositionCalculator;



    /// <summary>
    /// Utility class providing helper methods for date-time operations and retry logic.
    /// </summary>
    public static partial class TimeUtils
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(TimeUtils));
        
        /// <summary>
        /// Gets the current date and time in the London timezone.
        /// </summary>
        /// <returns>The current DateTime in the Europe/London timezone.</returns>
        public static DateTime GetLondonTime()
        {
            try
            {
                // Get timezone info for London
                TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

                // Get the current UTC time
                DateTime utcNow = DateTime.UtcNow;

                // Convert UTC time to London time
                DateTime londonNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, londonTimeZone);

                _logger?.Debug("Retrieved current London time: {LondonNow}", londonNow);
                return londonNow;
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger?.Error(ex, "London timezone not found on this system.");
                throw;
            }
            catch (InvalidTimeZoneException ex)
            {
                _logger?.Error(ex, "London timezone data is invalid.");
                throw;
            }
        }


    }

