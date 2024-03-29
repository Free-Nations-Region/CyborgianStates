﻿using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandTests;
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
            Assert.Throws<ArgumentNullException>(() => builder.WithField("name", null));
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

        [Fact]
        public async Task TestMessageReplyAsync()
        {
            var builder = new ConsoleResponseBuilder()
                .Failed("Test");
            var response = builder.Build();
            var command = BaseCommandTests.GetSlashCommand(new());
            var message = new Message(0, "", new ConsoleMessageChannel(), command.Object);
            await message.ReplyAsync(response);
            await message.ReplyAsync("test");

            message = new Message(0, "", new ConsoleMessageChannel());
            await message.ReplyAsync(response);
            await message.ReplyAsync("test");

            command.SetupGet(m => m.HasResponded).Returns(false);
            message = new Message(0, "", new ConsoleMessageChannel(), command.Object);
            await message.ReplyAsync(response);
            await message.ReplyAsync("test");
        }

        [Fact]
        public void DiscordResponseBuilder_Clear_Successfully()
        {
            var builder = new DiscordResponseBuilder()
               .Failed("Reason")
               .WithTitle("Title")
               .WithThumbnailUrl("https://localhost/image.jpg")
               .WithDescription("Description")
               .WithDefaults("Footer")
               .WithField("Key1", "Value")
               .WithContent("BlaBla")
               .WithUrl("https://localhost");
            var response = builder.Build();
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().Be("BlaBla");
            response.ResponseObject.Should().NotBeNull();
            response.ResponseObject.Should().BeAssignableTo<Embed>();
            builder.Clear();
            response = builder.Build();
            response.ResponseObject.Should().BeNull();
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().BeNull();
        }

        [Fact]
        public void ConsoleResponseBuilder_Clear_Successfully()
        {
            var builder = new ConsoleResponseBuilder()
               .Failed("Reason")
               .WithTitle("Title")
               .WithThumbnailUrl("https://localhost/image.jpg")
               .WithDescription("Description")
               .WithDefaults("Footer")
               .WithField("Key1", "Value")
               .WithContent("BlaBla")
               .WithUrl("https://localhost");
            var response = builder.Build();
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().Contain("BlaBla");
            builder.Clear();
            response = builder.Build();
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().BeEmpty();
        }
    }
}
