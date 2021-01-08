﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using CyborgianStates.Exceptions;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CyborgianStates.Commands
{
    public class NationStatsCommand : ICommand
    {
        private readonly AppSettings _config;
        private readonly IRequestDispatcher _dispatcher;
        private readonly IResponseBuilder _responseBuilder;
        private readonly ILogger _logger;
        private CancellationToken token;

        public NationStatsCommand()
        {
            _logger = ApplicationLogging.CreateLogger(typeof(NationStatsCommand));
            _dispatcher = (IRequestDispatcher) Program.ServiceProvider.GetService(typeof(IRequestDispatcher));
            _config = ((IOptions<AppSettings>) Program.ServiceProvider.GetService(typeof(IOptions<AppSettings>))).Value;
            _responseBuilder = (IResponseBuilder) Program.ServiceProvider.GetService(typeof(IResponseBuilder));
        }

        public async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            Request request = new Request(RequestType.GetBasicNationStats, ResponseFormat.XmlResult, DataSourceType.NationStatesAPI);
            try
            {
                _logger.LogDebug($"{message.Content}");
                var parameters = message.Content.Split(" ").Skip(1);
                if (parameters.Any())
                {
                    string nationName = string.Join(" ", parameters);
                    request.Params.Add("nationName", Helpers.ToID(nationName));
                    _dispatcher.Dispatch(request, 0);
                    await request.WaitForResponseAsync(token).ConfigureAwait(false);
                    if (request.Status == RequestStatus.Canceled)
                    {
                        return await FailCommandAsync(message, "Request has been canceled. Sorry :(").ConfigureAwait(false);
                    }
                    else if (request.Status == RequestStatus.Failed)
                    {
                        return await FailCommandAsync(message, request.FailureReason).ConfigureAwait(false);
                    }
                    else
                    {
                        CommandResponse commandResponse = ParseResponse(request);
                        await message.Channel.ReplyToAsync(message, commandResponse).ConfigureAwait(false);
                        return commandResponse;
                    }
                }
                else
                {
                    return await FailCommandAsync(message, "No parameter passed.").ConfigureAwait(false);
                }
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.ToString());
                return await FailCommandAsync(message, "Could not execute command. Something went wrong :(").ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                _logger.LogError(e.ToString());
                return await FailCommandAsync(message, "Request/Command has been canceled. Sorry :(").ConfigureAwait(false);
            }
            catch (HttpRequestFailedException e)
            {
                _logger.LogError(e.ToString());
                return await FailCommandAsync(message, request.FailureReason).ConfigureAwait(false);
            }
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            token = cancellationToken;
        }

        private async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        {
            _responseBuilder.WithColor(Discord.Color.Red)
                .FailWithDescription(reason)
                .WithFooter(_config.Footer);
            var response = _responseBuilder.Build();
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }

        private CommandResponse ParseResponse(Request request)
        {
            if (request.ExpectedReponseFormat == ResponseFormat.XmlResult && request.Response is XmlDocument nationStats)
            {
                string name = request.Params["nationName"].ToString();
                string demonymplural = nationStats.GetElementsByTagName("DEMONYM2PLURAL")[0].InnerText;
                string category = nationStats.GetElementsByTagName("CATEGORY")[0].InnerText;
                string flagUrl = nationStats.GetElementsByTagName("FLAG")[0].InnerText;
                string fullname = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                string population = nationStats.GetElementsByTagName("POPULATION")[0].InnerText;
                string region = nationStats.GetElementsByTagName("REGION")[0].InnerText;
                string founded = nationStats.GetElementsByTagName("FOUNDED")[0].InnerText;
                string lastActivity = nationStats.GetElementsByTagName("LASTACTIVITY")[0].InnerText;
                string Influence = nationStats.GetElementsByTagName("INFLUENCE")[0].InnerText;
                string wa = nationStats.GetElementsByTagName("UNSTATUS")[0].InnerText;
                XmlNodeList freedom = nationStats.GetElementsByTagName("FREEDOM")[0].ChildNodes;
                string civilStr = freedom[0].InnerText;
                string economyStr = freedom[1].InnerText;
                string politicalStr = freedom[2].InnerText;
                XmlNodeList census = nationStats.GetElementsByTagName("CENSUS")[0].ChildNodes;
                string civilRights = census[0].ChildNodes[0].InnerText;
                string economy = census[1].ChildNodes[0].InnerText;
                string politicalFreedom = census[2].ChildNodes[0].InnerText;
                string influenceValue = census[3].ChildNodes[0].InnerText;
                string endorsementCount = census[4].ChildNodes[0].InnerText;
                string residency = census[5].ChildNodes[0].InnerText;
                double residencyDbl = Convert.ToDouble(residency, _config.CultureInfo);
                int residencyYears = (int) (residencyDbl / 365.242199);
                int residencyDays = (int) (residencyDbl % 365.242199);
                double populationdbl = Convert.ToDouble(population, _config.CultureInfo);
                string nationUrl = $"https://www.nationstates.net/nation={Helpers.ToID(name)}";
                string regionUrl = $"https://www.nationstates.net/region={Helpers.ToID(region)}";
                string waVoteString = "";
                if (wa == "WA Member")
                {
                    var gaVote = nationStats.GetElementsByTagName("GAVOTE")[0].InnerText;
                    var scVote = nationStats.GetElementsByTagName("SCVOTE")[0].InnerText;
                    if (!string.IsNullOrWhiteSpace(gaVote))
                    {
                        waVoteString += $"GA Vote: {gaVote} | ";
                    }
                    if (!string.IsNullOrWhiteSpace(scVote))
                    {
                        waVoteString += $"SC Vote: {scVote} | ";
                    }
                }
                _responseBuilder.Success()
                    .WithTitle("BasicStats for Nation")
                    .WithThumbnailUrl(flagUrl)
                    .WithDescription($"**[{fullname}]({nationUrl})**{Environment.NewLine}" +
                    $"{(populationdbl / 1000.0 < 1 ? populationdbl : populationdbl / 1000.0).ToString(_config.CultureInfo)} {(populationdbl / 1000.0 < 1 ? "million" : "billion")} {demonymplural} | " +
                    $"Founded {founded} | " +
                    $"Last active {lastActivity}")
                    .WithField("Region", $"[{region}]({regionUrl})", true)
                    .WithField("Residency", $"{(residencyYears < 1 ? "" : $"{residencyYears} year" + $"{(residencyYears > 1 ? "s" : "")}")} " +
                    $"{residencyDays} { (residencyDays > 1 ? $"days" : "day")}", true)
                    .WithField(category, $"C: { civilStr} ({ civilRights}) | E: { economyStr} ({ economy}) | P: { politicalStr} ({ politicalFreedom})")
                    .WithField(wa, $"{waVoteString} {endorsementCount} endorsements | {influenceValue} Influence ({Influence})")
                    .WithDefaults(_config.Footer);
                return _responseBuilder.Build();
            }
            else
            {
                throw new InvalidOperationException("Expected Response to be XmlDocument but wasn't.");
            }
        }
    }
}