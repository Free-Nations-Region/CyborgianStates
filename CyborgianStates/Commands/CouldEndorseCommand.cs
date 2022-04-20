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
    public class CouldEndorseCommand : ICommand
    {
        private readonly AppSettings _config;
        private readonly IResponseBuilder _responseBuilder;
        private readonly IDumpDataService _dumpDataService;
        private readonly ILogger _logger;
        private CancellationToken token;
        
        public CouldEndorseCommand() : this(Program.ServiceProvider)
        {
        }

        public CouldEndorseCommand(IServiceProvider serviceProvider)
        {
            _logger = Log.ForContext<CouldEndorseCommand>();
            _config = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            _responseBuilder = serviceProvider.GetRequiredService<IResponseBuilder>();
            _dumpDataService = serviceProvider.GetRequiredService<IDumpDataService>();
        }

        public async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentException(nameof(message));
            }

            try
            {
                _logger.Debug(message.Content);
                var parameters = message.Content.Split(" ").Skip(1);

                // Checks for parameter count and extracts the first parameter or throws an exception.
                string nation = parameters.Count() == 1
                    ? Helpers.ToID(string.Join(" ", parameters))
                    : throw new Exception();
                
                await ProcessResultAsync(message, nation).ConfigureAwait(true);
                
                CommandResponse commandResponse = _responseBuilder.Build();
                await message.Channel.ReplyToAsync(message, commandResponse).ConfigureAwait(false);
                return commandResponse;
            }
            catch (InvalidOperationException e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message, "Specified nation is not a WA member.").ConfigureAwait(false);
            }
                catch (Exception e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message,
                    "An unexpected error occured. Please contact the bot administrator.").ConfigureAwait(false);
            }
        }
        
        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            token = cancellationToken;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nation"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task ProcessResultAsync(Message message, string nation)
        {
            DumpNation dumpNation = _dumpDataService.GetNationByName(nation);
            if (!dumpNation.IsWAMember)
            {
                throw new InvalidOperationException("Not a WA member."); // Guard clause
            }

            List<DumpNation> couldEndorse = _dumpDataService.GetWANationsByRegionName(dumpNation.RegionName); // Get all WA nations in the same region.
            couldEndorse.RemoveAll(n => dumpNation.Endorsements.Contains(n.Name)); // Remove all nations that already have endorsements by the dumpNation.
            couldEndorse.Remove(dumpNation); // Remove the nation itself.
            // Convert the list of nations to a string.
            List<string> couldEndorseNames = couldEndorse.Select(n => n.Name).ToList();
            await SplitResponseAsync(message, dumpNation, couldEndorseNames).ConfigureAwait(true);

            DumpRetrievalBackgroundService dumpService = new DumpRetrievalBackgroundService();
            int? hoursSinceUpdate = GetUpdateTime(dumpService, UpdateTime.Last)?.Hours;
            int? hoursUntilUpdate = GetUpdateTime(dumpService, UpdateTime.Next)?.Hours;

            _responseBuilder.Clear();
            _responseBuilder.Success()
                .WithField("Datascource", "Dump", true)
                .WithField("As of", $"{hoursSinceUpdate}h ago", true)
                .WithField("Next update in", $"{hoursUntilUpdate}h", true)
                .WithFooter(_config.Footer);
        }
        
        
        private async Task SplitResponseAsync(Message message, DumpNation nation, List<string> endorsable)
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
                return;
            }
            
            // Send out embeds in chunks of 3800 characters from the stack.
            while (endorse.Count > 0)
            {
                // Get the next chunk of 3800 characters.
                List<string> chunk = new List<string>();
                int chunkSize = 0;
                while (chunkSize < limit && endorse.Count > 0)
                {
                    chunk.Add(endorse.Pop());
                    chunkSize += chunk.Last().Length;
                }
                
                // Build the embed, send it, and reset it.
                _responseBuilder.Success()
                    .WithTitle($"{nation.Name} could endorse {endorsable.Count} more nations.")
                    .WithDescription(string.Join(", ", chunk));
                await message.Channel.ReplyToAsync(message, _responseBuilder.Build()).ConfigureAwait(false);
                _responseBuilder.Clear();
            }
        }

        private enum UpdateTime
        {
            Last,
            Next
        }
        
        private TimeSpan? GetUpdateTime(DumpRetrievalBackgroundService cronSchedule, UpdateTime nextOrLast)
        {
            var exp = new CronExpression(cronSchedule.CronSchedule) { TimeZone = cronSchedule.TimeZone };
            var next = exp.GetTimeAfter(DateTimeOffset.UtcNow);
            var nextDistance = DateTimeOffset.UtcNow - next;
            var last = exp.GetTimeBefore(DateTimeOffset.UtcNow);
            var lastDistance = last - DateTimeOffset.UtcNow;

            if (!next.HasValue && !last.HasValue)
            {
                return null;
            }
            
            return nextOrLast == UpdateTime.Next ? nextDistance : lastDistance;
        }

        private async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        {
            _responseBuilder.Clear();
            var response = _responseBuilder.FailWithDescription(reason).Build();
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }
    }
    
    
}

