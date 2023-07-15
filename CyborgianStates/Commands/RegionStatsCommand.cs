using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NationStatesSharp;
using NationStatesSharp.Enums;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace CyborgianStates.Commands
{
    public class RegionStatsCommand : BaseCommand
    {
        private readonly ILogger _logger;
        private readonly IDumpDataService _dumpDataService;
        private readonly CultureInfo _locale;
        public RegionStatsCommand() : this(Program.ServiceProvider)
        {
        }

        public RegionStatsCommand(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = Log.Logger.ForContext<NationStatsCommand>();
            _dumpDataService = serviceProvider.GetRequiredService<IDumpDataService>();
            _locale = _config.CultureInfo;
        }
        public override async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            try
            {
                await message.DeferAsync().ConfigureAwait(false);
                if (_dumpDataService.Status == Enums.DumpDataStatus.Ready)
                {
                    string regionName = Helpers.ToID(GetRegionName(message));
                    return await GetRegionStatsResponseAsync(message, regionName).ConfigureAwait(false);
                }
                else
                {
                    return _dumpDataService.Status == Enums.DumpDataStatus.Updating
                        ? await FailCommandAsync(message, "Dump Information is currently updating. Please try again later. Avg. ETA: ~15 s", Discord.Color.Gold, "Dump Data updating").ConfigureAwait(false)
                        : await FailCommandAsync(message, $"Dump Data Service is in Status ({_dumpDataService.Status}) from which it can not recover on its own. Please contact the bot administrator.").ConfigureAwait(false);
                }
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

        private async Task<CommandResponse> GetRegionStatsResponseAsync(Message message, string regionName)
        {
            var request = new Request($"region={regionName}&q=name+numnations+founded+foundedtime+tags+delegate+census+power;mode=score;scale=65", ResponseFormat.Xml);
            _dispatcher.Dispatch(request, 0);
            await request.WaitForResponseAsync(_token).ConfigureAwait(false);
            await ProcessResultAsync(request, regionName).ConfigureAwait(false);
            CommandResponse commandResponse = _responseBuilder.Build();
            await message.ReplyAsync(commandResponse).ConfigureAwait(false);
            return commandResponse;
        }

        private static string GetRegionName(Message message)
        {
            string result = string.Empty;
            if (message.IsSlashCommand)
            {
                var commandParams = message.SlashCommand.Data.Options;
                result = (string) commandParams.FirstOrDefault(c => c.Name == "name")?.Value;
            }
            else if (message.Content.Contains(' '))
            {
                var parameters = message.Content.Split(" ").Skip(1);
                result = string.Join(" ", parameters);
            }
            return string.IsNullOrWhiteSpace(result) ? "The Free Nations Region" : result;
        }
        private async Task ProcessResultAsync(Request request, string regionName)
        {
            var dumpRegion = _dumpDataService.GetRegionByName(regionName);
            var dumpFounderNation = _dumpDataService.GetNationByName(dumpRegion.Founder);
            var dumpDelegateNation = _dumpDataService.GetNationByName(dumpRegion.Delegate);
            string regionUrl = $"https://www.nationstates.net/region={Helpers.ToID(regionName)}";
            var response = request.GetResponseAsXml();
            var name = response.GetFirstValueByNodeName("NAME");
            var numnations = response.GetFirstValueByNodeName("NUMNATIONS");
            var wadelegate = response.GetFirstValueByNodeName("DELEGATE");
            var founded = response.GetFirstValueByNodeName("FOUNDED");
            founded = founded == "0" ? "Antiquity" : founded;
            var foundedTime = response.GetFirstValueByNodeName("FOUNDEDTIME");
            var power = response.GetFirstValueByNodeName("POWER");
            var regionalAvgInfluence = response.Descendants("SCALE").Where(e => e.Attribute("id").Value == "65").FirstOrDefault()?.Value;
            var tags = response.Descendants().Where(e => e.Name == "TAG").Select(e => e.Value);
            var waNationsCount = _dumpDataService.GetWANationsByRegionName(regionName).Count;
            var endoCount = _dumpDataService.GetEndoSumByRegionName(regionName);

            var firstOffice = await GetFirstOfficeAndOfficerAsync(regionName).ConfigureAwait(false);
            _responseBuilder.Success()
                .WithTitle(name)
                .WithUrl(regionUrl)
                .WithThumbnailUrl(dumpRegion.Flag)
                .WithField("Founder", dumpFounderNation?.UnescapedName ?? "Unknown", true)
                .WithField("Founded", founded, true)
                .WithField("Nations", $"[{numnations}]({regionUrl}/page=list_nations)", true);
            if (firstOffice is not null && !string.IsNullOrWhiteSpace(firstOffice.Item1) && !string.IsNullOrWhiteSpace(firstOffice.Item2))
            {
                _responseBuilder.WithField(firstOffice.Item1, firstOffice.Item2, true);
            }
            if (!string.IsNullOrWhiteSpace(regionalAvgInfluence) && double.TryParse(regionalAvgInfluence, NumberStyles.Number, _locale, out double avgInfluenceValue) && int.TryParse(numnations, out int numnationsValue))
            {
                var powerValue = avgInfluenceValue * numnationsValue;
                var powerValueString = powerValue > 1000 ? (powerValue / 1000.0).ToString("0.000", _locale) + "k" : powerValue.ToString(_locale);
                _responseBuilder.WithField("Regional Power", $"{power} | {powerValueString} Points", true);
            }
            else
            {
                _responseBuilder.WithField("Regional Power", $"{power}", true);
            }
            var endoCountString = endoCount > 1000 ? (endoCount / 1000.0).ToString("0.000", _locale) + "k" : endoCount.ToString(_locale);
            _responseBuilder.WithField("World Assembly*", $"{waNationsCount} member{(waNationsCount > 1 ? "s" : string.Empty)} | {endoCountString} endos");
            if (!string.IsNullOrWhiteSpace(wadelegate) && wadelegate != "0")
            {
                _responseBuilder.WithField($"WA Delegate", $"[{dumpDelegateNation.UnescapedName}](https://www.nationstates.net/nation={Helpers.ToID(wadelegate)}) | {dumpDelegateNation.Endorsements.Count} endorsement(s) | Influence: {dumpDelegateNation.Influence}");
            }
            var tagString = string.Join(", ", tags);
            _responseBuilder.WithField("Tags", tagString);
            _responseBuilder.WithDefaults(_config.Footer);
        }

        private async Task<Tuple<string, string>> GetFirstOfficeAndOfficerAsync(string regionName)
        {
            var request = new Request($"region={Helpers.ToID(regionName)}&q=officers", ResponseFormat.Xml);
            _dispatcher.Dispatch(request, 0);
            await request.WaitForResponseAsync(_token).ConfigureAwait(false);
            var doc = request.GetResponseAsXml();
            var firstOffice = doc.GetParentsOfFilteredDescendants("ORDER", "1").FirstOrDefault();
            if (firstOffice is not null)
            {
                var nationName = firstOffice.Element("NATION")?.Value;
                var nation = _dumpDataService.GetNationByName(nationName);
                var res = new Tuple<string, string>(firstOffice.Element("OFFICE")?.Value, nation?.UnescapedName);
                return res;
            }
            else
            {
                return null;
            }
        }
    }
}