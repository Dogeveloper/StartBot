
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace StartBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                BotConfig.LoadConfig();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("A valid configuration file could not be found. Please create one at ./config.yml and enter in the appropriate values.");
                Environment.Exit(1);
            }
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public static DiscordSocketClient _client;
        public static IServiceProvider _services;

        public static IServiceProvider GetServices()
        {
            return _services;
        }
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .BuildServiceProvider();
            string botToken = BotConfig.GetCachedConfig().Discord.BotToken;
            _client.Log += _client_Log;
            RegisterEvents();
            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();
            await Task.Delay(-1);

        }

        public async Task Start()
        {
            if (_client.GetGuild(BotConfig.GetCachedConfig().Discord.GuildId) != null)
            {
                var guild = _client.GetGuild(BotConfig.GetCachedConfig().Discord.GuildId);
                if (guild.GetTextChannel(BotConfig.GetCachedConfig().Discord.MessageChannelId) != null)
                {
                    var chan = guild.GetTextChannel(BotConfig.GetCachedConfig().Discord.MessageChannelId);
                    if(BotConfig.GetCachedConfig().InternalMessageId == 0 || await chan.GetMessageAsync(BotConfig.GetCachedConfig().InternalMessageId) == null)
                    {
                        var msg = await chan.SendMessageAsync(embed: Embeds.ServerStopped());
                        await msg.AddReactionAsync(new Emoji("🔄"));

                        var cfg = BotConfig.GetCachedConfig();
                        cfg.InternalMessageId = (ulong) msg.Id;
                        BotConfig.SaveConfig(cfg);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find channel specified in the config file.");
                }
            }
            else
            {
                Console.WriteLine("Could not find guild specified in config file.");
            }
        }

        public void RegisterEvents()
        {
            _client.Ready += Start;
            _client.ReactionAdded += new ReactEventHandler().Handle;
        }

        private Task _client_Log(LogMessage l)
        {
            Console.WriteLine(l);
            return Task.CompletedTask;
        }
    }
}
