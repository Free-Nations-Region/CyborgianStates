using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandHandling;
using CyborgianStates.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NationStatesSharp.Interfaces;
using System;
using System.Threading.Tasks;

namespace CyborgianStates.Tests.CommandTests
{
    public abstract class BaseCommandTests
    {

        protected virtual ServiceCollection ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            var options = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            options.SetupGet(m => m.Value).Returns(new AppSettings() { SeperatorChar = '$', Locale = "en-US" });
            serviceCollection.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
            serviceCollection.AddSingleton<IResponseBuilder, ConsoleResponseBuilder>();
            serviceCollection.AddSingleton(typeof(IOptions<AppSettings>), options.Object);
            return serviceCollection;
        }

        public abstract Task TestExecuteSuccess();
        public abstract Task TestCancel();
        public abstract Task TestExecuteWithErrors();
    }
}