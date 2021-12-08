using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates
{
    public class ConsoleThemes
    {
        public static AnsiConsoleTheme Default { get; } = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = "\x1b[38;5;0015m",
            [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;0007m",
            [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;0238m",
            [ConsoleThemeStyle.Invalid] = "\x1b[38;5;0001m",
            [ConsoleThemeStyle.Null] = "\x1b[38;5;0038m",
            [ConsoleThemeStyle.Name] = "\x1b[38;5;0250m",
            [ConsoleThemeStyle.String] = "\x1b[38;5;0051m",
            [ConsoleThemeStyle.Number] = "\x1b[38;5;128m",
            [ConsoleThemeStyle.Boolean] = "\x1b[38;5;0020m",
            [ConsoleThemeStyle.Scalar] = "\x1b[38;5;0002m",
            [ConsoleThemeStyle.LevelVerbose] = "\x1b[38;5;0098m",
            [ConsoleThemeStyle.LevelDebug] = "\x1b[38;5;0007m",
            [ConsoleThemeStyle.LevelInformation] = "\x1b[38;5;0002m",
            [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;00011m",
            [ConsoleThemeStyle.LevelError] = "\x1b[38;5;0009m",
            [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;0196m",
        });
    }
}