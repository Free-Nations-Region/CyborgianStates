using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesSharp;
using NationStatesSharp.Enums;
using Serilog;
using ILogger = Serilog.ILogger;
using IRequestDispatcher = NationStatesSharp.Interfaces.IRequestDispatcher;

namespace CyborgianStates.Commands
{
    public class NationStatsCommand : BaseCommand
    {
        private readonly ILogger _logger;

        public NationStatsCommand() : base()
        {
        }

        public NationStatsCommand(IServiceProvider serviceProvider) : base(serviceProvider) => _logger = Log.ForContext<NationStatsCommand>();

        public override async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            try
            {
                await message.DeferAsync().ConfigureAwait(false);
                var nationName = GetNationName(message);
                return !string.IsNullOrWhiteSpace(nationName)
                    ? await GetNationStatsResponseAsync(message, Helpers.ToID(nationName)).ConfigureAwait(false)
                    : await FailCommandAsync(message, "No parameter passed.", Discord.Color.Red, "That didn't work.").ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message, "Request/Command has been canceled. Sorry :(").ConfigureAwait(false);
            }
            catch (HttpRequestFailedException e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message, e.Message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message, "An unexpected error occured. Please contact the bot administrator.").ConfigureAwait(false);
            }
        }

        private static string GetNationName(Message message)
        {
            if (message.IsSlashCommand)
            {
                var commandParams = message.SlashCommand.Data.Options;
                return (string) commandParams.FirstOrDefault(c => c.Name == "name")?.Value;
            }
            else if (message.Content.Contains(' '))
            {
                var parameters = message.Content.Split(" ").Skip(1);
                return string.Join(" ", parameters);
            }
            else
            {
                return null;
            }

        }

        private async Task<CommandResponse> GetNationStatsResponseAsync(Message message, string nationName)
        {
            var request = new Request($"nation={nationName}&q=flag+wa+gavote+scvote+fullname+freedom+demonym2plural+category+population+region+founded+foundedtime+influence+lastactivity+census;mode=score;scale=0+1+2+65+66+80", ResponseFormat.Xml);
            _dispatcher.Dispatch(request, 0);
            await request.WaitForResponseAsync(_token).ConfigureAwait(false);
            await ProcessResultAsync(request, nationName).ConfigureAwait(false);
            CommandResponse commandResponse = _responseBuilder.Build();
            await message.ReplyAsync(commandResponse).ConfigureAwait(false);
            return commandResponse;
        }

        //private async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        //{
        //    _responseBuilder.Clear();
        //    _responseBuilder.FailWithDescription(reason)
        //        .WithFooter(_config.Footer);
        //    var response = _responseBuilder.Build();
        //    await message.ReplyAsync(response).ConfigureAwait(false);
        //    return response;
        //}

        private async Task<string> GetOfficerPositionAsync(string nationName, string regionName)
        {
            var request = new Request($"region={Helpers.ToID(regionName)}&q=officers", ResponseFormat.Xml);
            _dispatcher.Dispatch(request, 0);
            await request.WaitForResponseAsync(_token).ConfigureAwait(false);
            var doc = request.GetResponseAsXml();

            var list = doc.GetParentsOfFilteredDescendants("NATION", nationName);
            return list.Any() ? (list.First().Element("OFFICE")?.Value) : string.Empty;
        }

        private async Task ProcessResultAsync(Request request, string nationName)
        {
            var response = request.GetResponseAsXml();
            string demonymplural = response.GetFirstValueByNodeName("DEMONYM2PLURAL");
            string category = response.GetFirstValueByNodeName("CATEGORY");
            string flagUrl = response.GetFirstValueByNodeName("FLAG");
            string fullname = response.GetFirstValueByNodeName("FULLNAME");
            string population = response.GetFirstValueByNodeName("POPULATION");
            string region = response.GetFirstValueByNodeName("REGION");
            string founded = response.GetFirstValueByNodeName("FOUNDED");
            string foundedtime = response.GetFirstValueByNodeName("FOUNDEDTIME");
            var dateFounded = DateTimeOffset.FromUnixTimeSeconds(long.Parse(foundedtime)).UtcDateTime;
            string lastActivity = response.GetFirstValueByNodeName("LASTACTIVITY");
            string Influence = response.GetFirstValueByNodeName("INFLUENCE");
            string wa = response.GetFirstValueByNodeName("UNSTATUS");
            var freedom = response.Descendants("FREEDOM").Elements();

            string civilStr = freedom.ElementAtOrDefault(0)?.Value;
            string economyStr = freedom.ElementAtOrDefault(1)?.Value;
            string politicalStr = freedom.ElementAtOrDefault(2)?.Value;

            var census = response.Descendants("CENSUS").Elements();
            string civilRights = census.ElementAtOrDefault(0)?.Element("SCORE")?.Value;
            string economy = census.ElementAtOrDefault(1)?.Element("SCORE")?.Value;
            string politicalFreedom = census.ElementAtOrDefault(2)?.Element("SCORE")?.Value;
            string influenceValue = census.ElementAtOrDefault(3)?.Element("SCORE")?.Value;
            string endorsementCount = census.ElementAtOrDefault(4)?.Element("SCORE")?.Value;
            string residency = census.ElementAtOrDefault(5)?.Element("SCORE")?.Value;

            double residencyDbl = Convert.ToDouble(residency, _config.CultureInfo);
            var dateJoined = DateTime.UtcNow.Subtract(TimeSpan.FromDays(residencyDbl));
            int residencyYears = (int) (residencyDbl / 365.242199);
            int residencyDays = (int) (residencyDbl % 365.242199);
            double populationdbl = Convert.ToDouble(population, _config.CultureInfo);
            string nationUrl = $"https://www.nationstates.net/nation={Helpers.ToID(nationName)}";
            string regionUrl = $"https://www.nationstates.net/region={Helpers.ToID(region)}";
            string waVoteString = "";
            string endoString = "";
            if (wa == "WA Member")
            {
                var gaVote = response.GetFirstValueByNodeName("GAVOTE");
                var scVote = response.GetFirstValueByNodeName("SCVOTE");
                if (!string.IsNullOrWhiteSpace(gaVote))
                {
                    waVoteString += $"GA: {gaVote}";
                }
                if (!string.IsNullOrWhiteSpace(waVoteString))
                {
                    waVoteString += " | ";
                }
                if (!string.IsNullOrWhiteSpace(scVote))
                {
                    waVoteString += $"SC: {scVote}";
                }
                endoString = $"{endorsementCount.Split('.')[0]} endorsements |";
            }
            var officerPosition = await GetOfficerPositionAsync(nationName, region).ConfigureAwait(false);
            _responseBuilder.Success()
                .WithTitle(fullname)
                .WithUrl(nationUrl)
                .WithThumbnailUrl(flagUrl)
                .WithDescription($"{(populationdbl / 1000.0 < 1 ? populationdbl : populationdbl / 1000.0).ToString(_config.CultureInfo)} {(populationdbl / 1000.0 < 1 ? "million" : "billion")} {demonymplural} | " +
                $"Last active {lastActivity}")
                .WithField("Founded", $"{dateFounded:dd.MM.yyyy} ({founded})")
                .WithField("Region", $"[{region}]({regionUrl})", true);
            if (!string.IsNullOrWhiteSpace(officerPosition))
            {
                _responseBuilder.WithField("Regional Officer", officerPosition, true);
            }
            _responseBuilder.WithField("Resident Since", $"{dateJoined:dd.MM.yyyy} ({(residencyYears < 1 ? "" : $"{residencyYears} y ")}{residencyDays} d)", string.IsNullOrWhiteSpace(officerPosition));
            _responseBuilder.WithField(category, $"C: {civilStr} ({civilRights}) | E: {economyStr} ({economy}) | P: {politicalStr} ({politicalFreedom})")
                .WithField(wa, $"{endoString} {influenceValue} Influence ({Influence})");
            if (!string.IsNullOrWhiteSpace(waVoteString))
            {
                _responseBuilder.WithField("WA Vote", waVoteString);
            }
            _responseBuilder.WithField("Links", $"[Dispatches](https://www.nationstates.net/page=dispatches/nation={nationName})  |  [Cards Deck](https://www.nationstates.net/page=deck/nation={nationName})  |  [Challenge](https://www.nationstates.net/page=challenge?entity_name={nationName})")
            .WithDefaults(_config.Footer);
        }
    }
}