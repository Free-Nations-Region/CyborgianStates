using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandHandling;
using CyborgianStates.Tests.Helpers;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NationStatesSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.CommandTests
{
    public abstract class BaseCommandTests
    {

        protected virtual ServiceCollection ConfigureServices(IRequestDispatcher dispatcher)
        {
            var serviceCollection = new ServiceCollection();
            var options = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            options.SetupGet(m => m.Value).Returns(new AppSettings() { SeperatorChar = '$', Locale = "en-US", Contact = "" });
            serviceCollection.AddSingleton<IRequestDispatcher>(dispatcher);
            serviceCollection.AddSingleton<IResponseBuilder, ConsoleResponseBuilder>();
            serviceCollection.AddSingleton(typeof(IOptions<AppSettings>), options.Object);
            return serviceCollection;
        }
        [Fact]
        public abstract Task TestExecuteSuccess();
        [Fact]
        public abstract Task TestCancel();
        [Fact]
        public abstract Task TestExecuteWithErrors();

        internal static Mock<ISlashCommand> GetSlashCommand(Dictionary<string, object> parameters, string content = "", IUser user = null)
        {
            var mockCommand = new Mock<ISlashCommand>(MockBehavior.Strict);

            var mockInteractionData = new Mock<IApplicationCommandInteractionData>(MockBehavior.Strict);
            mockInteractionData.SetupGet(i => i.Options).Returns(GetOptions(parameters));
            mockInteractionData.SetupGet(i => i.Name).Returns(content);
            mockCommand.SetupGet(i => i.CommandName).Returns(content);
            mockCommand.SetupGet(i => i.User).Returns(user);
            mockCommand.SetupGet(p => p.Data).Returns(mockInteractionData.Object);
            mockCommand.SetupGet(p => p.HasResponded).Returns(true);
            mockCommand.SetupGet(p => p.Channel).Returns(() => null);
            mockCommand.Setup(p => p.ModifyOriginalResponseAsync(It.IsAny<Action<MessageProperties>>(), null)).Returns(() => Task.FromResult<IUserMessage>(null));
            mockCommand.Setup(p => p.DeferAsync(false, null)).Returns(() => Task.CompletedTask);
            mockCommand.Setup(p => p.RespondAsync(It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<AllowedMentions>(), It.IsAny<MessageComponent>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>())).Returns(() => Task.FromResult<IUserMessage>(null)); ;
            return mockCommand;

            static List<IApplicationCommandInteractionDataOption> GetOptions(Dictionary<string, object> parameters)
            {
                var list = new List<IApplicationCommandInteractionDataOption>();
                foreach (var kvp in parameters)
                {
                    var mockParamInteractionData = new Mock<IApplicationCommandInteractionDataOption>(MockBehavior.Strict);
                    mockParamInteractionData.SetupGet(i => i.Name).Returns(kvp.Key);
                    mockParamInteractionData.SetupGet(i => i.Value).Returns(kvp.Value);
                    list.Add(mockParamInteractionData.Object);
                }
                return list;
            }
        }
    }
}