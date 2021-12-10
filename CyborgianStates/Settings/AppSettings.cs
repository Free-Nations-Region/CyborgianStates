using System.Globalization;
using System.Runtime.CompilerServices;
using CyborgianStates.Enums;

[assembly: InternalsVisibleTo("CyborgianStates.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace CyborgianStates
{
    public class AppSettings
    {
        public const string VERSION = "v4.0.0-beta";
        public const ulong FNR_GUILD_ID = 241904885497462784;

        static internal bool IsTesting = false;
        private static string config = "development";

        public string Footer => $"CyborgianStates {VERSION} by drehtisch · See {SeperatorChar}about";

        public static string Configuration
        {
            get
            {
                if (IsTesting)
                {
                    return config;
                }
                else
                {
#if RELEASE
                    return "production";
#elif DEBUG
                    return "development";
#endif
                }
            }
            internal set
            {
                config = value;
            }
        }

        public string Contact { get; set; }
        public string DbConnection { get; set; }
        public ulong ExternalAdminUserId { get; set; }
        public string DiscordBotLoginToken { get; set; }
        public ulong PrimaryGuildId => FNR_GUILD_ID;
        public InputChannel InputChannel { get; set; }

        public CultureInfo CultureInfo
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Locale) ? new CultureInfo(Locale) : CultureInfo.InvariantCulture;
            }
        }

        public string Locale { get; set; }
        public char SeperatorChar { get; set; }
        public bool ArchiveDumps { get; set; }
    }
}