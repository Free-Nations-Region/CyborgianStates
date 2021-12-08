using CyborgianStates.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace CyborgianStates
{
    public static class Helpers
    {
        /// <summary>
        /// Converts a API Id back to nation/region name
        /// </summary>
        /// <param name="text">The API Id to format back</param>
        /// <returns>Formated string convert back to name</returns>
        public static string FromID(string text)
        {
            return text?.Trim().ToLower(CultureInfo.InvariantCulture).Replace('_', ' ');
        }

        /// <summary>
        /// Creates a EventId from LoggingEvent
        /// </summary>
        /// <param name="loggingEvent">LoggingEvent to create a EventId from.</param>
        /// <returns>The created EventId</returns>
        public static EventId GetEventIdByType(LoggingEvent loggingEvent)
        {
            return new EventId((int) loggingEvent, loggingEvent.ToString());
        }

        /// <summary>
        /// Converts nation/region name to format that can be used on api calls
        /// </summary>
        /// <param name="text">The text to ensure format on</param>
        /// <returns>Formated string</returns>
        public static string ToID(string text)
        {
            return text?.Trim().ToLower(CultureInfo.InvariantCulture).Replace(' ', '_').Trim('@');
        }
    }
}