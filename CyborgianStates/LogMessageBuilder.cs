﻿using Microsoft.Extensions.Logging;

namespace CyborgianStates
{
    public static class LogMessageBuilder
    {
        public static string Build(EventId eventId, string message) => $"[{eventId.Id}] {message}";
    }
}