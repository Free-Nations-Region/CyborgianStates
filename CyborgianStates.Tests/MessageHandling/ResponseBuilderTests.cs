using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Discord;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.MessageHandling
{
    public class ResponseBuilderTests
    {
        [Fact]
        public void TestConsoleResponseBuilderWithExtensions()
        {
            var builder = new ConsoleResponseBuilder()
                .Failed("Test");
            var response = builder.Build();
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith("Test");

            response = builder.Success()
                .WithContent("Test2")
                .Build();
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().StartWith("Test2");

            response = builder.Success()
                .WithField("Key", "Value", true)
                .WithField("Key1", "Value")
                .WithDescription("Description")
                .WithTitle("Title")
                .WithFooter("Footer")
                .Build();
            response.Content.Should().ContainAll(new List<string>() { "Key", "Key1", "Value", "Description", "Title", "Footer" });

            response = builder.FailWithDescription("Reason")
                .WithDefaults("Footer")
                .Build();

            response.Content.Should().ContainAll(new List<string>() { "Something went wrong", "Reason", "Footer"});
        }

        [Fact]
        public void TestDiscordResponseBuilder()
        {
            var builder = new DiscordResponseBuilder()
                .Success()
                .WithTitle("Title")
                .WithThumbnailUrl("https://localhost/image.jpg")
                .WithDescription("Description")
                .WithDefaults("Footer")
                .WithField("Key1", "Value")
                .WithUrl("https://localhost");
            var response = builder.Build();
            response.Status.Should().Be(CommandStatus.Success);
            response.ResponseObject.Should().NotBeNull();
            response.ResponseObject.Should().BeAssignableTo<Embed>();
        }
    }
}
