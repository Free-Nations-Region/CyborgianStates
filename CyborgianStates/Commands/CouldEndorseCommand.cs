using CyborgianStates.CommandHandling;
using CyborgianStates.Data.Models.Dump;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public class CouldEndorseCommand(IServiceProvider serviceProvider) : BaseCommand
    {
        private readonly AppSettings _config = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
        private readonly IResponseBuilder _responseBuilder = serviceProvider.GetRequiredService<IResponseBuilder>();
        private readonly IDumpDataService _dumpDataService = serviceProvider.GetRequiredService<IDumpDataService>();
        private readonly DumpRetrievalBackgroundService _dumpRetrievalBackgroundService = serviceProvider.GetRequiredService<DumpRetrievalBackgroundService>();
        private readonly ILogger _logger = Log.ForContext<CouldEndorseCommand>();
        private CancellationToken token;

        public CouldEndorseCommand() : this(Program.ServiceProvider)
        {
        }

        public override async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentException(null, nameof(message));
            }

            var nationName = GetNationName(message);
            try
            {
                await message.DeferAsync().ConfigureAwait(false);
                if (nationName == null)
                {
                    _logger.Error("Required parameter 'nationName' was not specified.");
                    return await FailCommandAsync(message, "Required parameter 'nationName' was not specified.").ConfigureAwait(false);
                }

                await ProcessResultAsync(message, nationName).ConfigureAwait(true);

                CommandResponse commandResponse = _responseBuilder.Build();
                await message.ReplyAsync(commandResponse).ConfigureAwait(false);
                return commandResponse;
            }
            catch (InvalidOperationException e)
            {
                _logger.Error(e, "Found nation '{name}' is not apart of the World Assembly.", nationName);
                return await FailCommandAsync(message, $"Specified nation '{nationName}' is not a WA member.").ConfigureAwait(false);
            }
            catch (KeyNotFoundException e)
            {
                _logger.Error(e, "Nation '{name}' cannot be found.", nationName);
                return await FailCommandAsync(message, $"Specified nation '{nationName}' could not be found.").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while executing CouldEndorseCommand");
                return await FailCommandAsync(message,
                    "An unexpected error occured. Please contact the bot administrator.").ConfigureAwait(false);
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

        public void SetCancellationToken(CancellationToken cancellationToken) => token = cancellationToken;

        private async Task ProcessResultAsync(Message message, string nation)
        {
            DumpNation dumpNation = _dumpDataService.GetNationByName(nation);
            if (dumpNation == null)
            {
                throw new KeyNotFoundException(nation);
            }
            if (!dumpNation.IsWAMember)
            {
                throw new InvalidOperationException("Not a WA member."); // Guard clause
            }

            List<DumpNation> couldEndorse = _dumpDataService.GetWANationsByRegionName(dumpNation.RegionName); // Get all WA nations in the same region.
            couldEndorse.RemoveAll(n => n.Endorsements.Contains(dumpNation.Name)); // Remove all nations that already have endorsements by the dumpNation.
            couldEndorse.Remove(dumpNation); // Remove the nation itself.
            // Convert the list of nations to a string.
            var couldEndorseNames = couldEndorse.Select(n => n.Name).ToList();
            var responseSplitted = await SplitResponseAsync(message, dumpNation, couldEndorseNames).ConfigureAwait(true);

            TimeSpan? hoursSinceUpdate = GetUpdateTime(_dumpRetrievalBackgroundService, UpdateTime.Last);
            TimeSpan? hoursUntilUpdate = GetUpdateTime(_dumpRetrievalBackgroundService, UpdateTime.Next);

            if (responseSplitted)
            {
                _responseBuilder.Success()
                    .WithTitle("Finished")
                    .WithFooter(_config.Footer);
            }
            _responseBuilder.WithField("Datasource", "Dump", true)
                    .WithField("As of", $"{(hoursSinceUpdate.HasValue ? $"{hoursSinceUpdate.Value.Hours:00}:{hoursSinceUpdate.Value.Minutes:00}:{hoursSinceUpdate.Value.Seconds:00}" : "Unknown")} ago", true)
                    .WithField("Next update in", $"{(hoursUntilUpdate.HasValue ? $"{hoursUntilUpdate.Value.Hours:00}:{hoursUntilUpdate.Value.Minutes:00}:{hoursUntilUpdate.Value.Seconds:00}" : "Unknown")}", true);
        }


        private async Task<bool> SplitResponseAsync(Message message, DumpNation nation, List<string> endorsable)
        {
            const int limit = 3800; // Less than 4096 description limit to accommodate for ", " in string.Join.

            Stack<string> endorse = new Stack<string>(endorsable.Select(n => $"[{n}](https://www.nationstates.net/nation={n})"));

            int totalChars = endorse.Sum(n => n.Length);
            if (totalChars < limit) // Determine if we need to split the response at all.
            {
                _responseBuilder.Success()
                    .WithTitle($"{nation.Name} could endorse {endorsable.Count} more nations.")
                    .WithDescription(string.Join(", ", endorse))
                    .WithFooter(_config.Footer);

                return false;
            }

            // Send out embeds in chunks of 3800 characters from the stack.
            while (endorse.Count > 0)
            {
                // Get the next chunk of 3800 characters.
                var chunk = new List<string>();
                int chunkSize = 0;
                while (chunkSize < limit && endorse.Count > 0)
                {
                    string nextString = endorse.Pop();
                    chunk.Add(nextString);
                    chunkSize += nextString.Length;
                }

                // Build the embed, send it, and reset it.
                _responseBuilder.Success()
                    .WithTitle($"{nation.Name} could endorse {endorsable.Count} more nations.")
                    .WithDescription(string.Join(", ", chunk));
                await message.Channel.ReplyToAsync(message, _responseBuilder.Build()).ConfigureAwait(true);
                _responseBuilder.Clear();
            }
            return true;
        }

        private enum UpdateTime
        {
            Last,
            Next
        }

        // TODO: This method should be moved elsewhere so that it can be used by other commands.
        private TimeSpan? GetUpdateTime(DumpRetrievalBackgroundService cronSchedule, UpdateTime nextOrLast)
        {
            var exp = new CronExpression(cronSchedule.CronSchedule) { TimeZone = cronSchedule.TimeZone };
            var next = exp.GetTimeAfter(DateTimeOffset.UtcNow);
            var nextDistance = next - DateTimeOffset.UtcNow;
            var last = exp.GetTimeBefore(DateTimeOffset.UtcNow);
            var lastDistance = last - DateTimeOffset.UtcNow;

            if (!next.HasValue && !last.HasValue)
            {
                return null;
            }

            return nextOrLast == UpdateTime.Next ? nextDistance : lastDistance;
        }

        //private async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        //{
        //    _responseBuilder.Clear();
        //    var response = _responseBuilder.FailWithDescription(reason).Build();
        //    await message.ReplyToAsync(message, response).ConfigureAwait(false);
        //    return response;
        //}
    }


}

