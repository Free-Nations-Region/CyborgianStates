using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CyborgianStates.Wrapper
{
    public class DiscordClientWrapper : DiscordSocketClient
    {
        public DiscordClientWrapper() : base() { }
        public virtual bool IsTest { get; } = false;
        public DiscordClientWrapper(DiscordSocketConfig config) : base(config) { }

        public new virtual Task StartAsync() => IsTest ? Task.CompletedTask : base.StartAsync();

        public new virtual Task StopAsync() => IsTest ? Task.CompletedTask : base.StopAsync();

        public new virtual Task LogoutAsync() => IsTest ? Task.CompletedTask : base.StopAsync();

        public new virtual Task LoginAsync(TokenType tokenType, string token, bool validateToken = true) => IsTest ? Task.CompletedTask : base.LoginAsync(tokenType, token, validateToken);

        public new virtual void Dispose() => base.Dispose();

        public new virtual event Func<Task> Ready
        {
            add
            {
                if (!IsTest)
                {
                    base.Ready += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.Ready -= value;
                }
            }
        }

        public new virtual event Func<Task> Connected
        {
            add
            {
                if (!IsTest)
                {
                    base.Connected += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.Connected -= value;
                }
            }
        }

        public new virtual event Func<Exception, Task> Disconnected
        {
            add
            {
                if (!IsTest)
                {
                    base.Disconnected += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.Disconnected -= value;
                }
            }
        }

        public new virtual event Func<Task> LoggedIn
        {
            add
            {
                if (!IsTest)
                {
                    base.LoggedIn += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.LoggedIn -= value;
                }
            }
        }

        public new virtual event Func<Task> LoggedOut
        {
            add
            {
                if (!IsTest)
                {
                    base.LoggedOut += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.LoggedOut -= value;
                }
            }
        }

        public new virtual event Func<LogMessage, Task> Log
        {
            add
            {
                if (!IsTest)
                {
                    base.Log += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.Log -= value;
                }
            }
        }

        public new virtual event Func<SocketMessage, Task> MessageReceived
        {
            add
            {
                if (!IsTest)
                {
                    base.MessageReceived += value;
                }
            }
            remove
            {
                if (!IsTest)
                {
                    base.MessageReceived -= value;
                }
            }
        }
    }
}