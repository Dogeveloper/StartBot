
using Amazon.EC2.Model;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceLd.MineStatSharp;

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
                //Environment.Exit(1);
            }
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public static DiscordSocketClient _client;
        public static IServiceProvider _services;


        public static async Task ModLog(string title, string description)
        {
            var chan = _client.GetChannel(831964996287332352) as SocketTextChannel;

            var embed = new EmbedBuilder().WithTitle(title).WithDescription(description).WithColor(Color.Blue).Build();

            await chan.SendMessageAsync(embed: embed);
        }

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

                var deferStopCommand = new SlashCommandBuilder();
                var forceStartInstanceBuilder = new SlashCommandBuilder();
                var forceStopInstanceBuilder = new SlashCommandBuilder();
                var forceSyncBuilder = new SlashCommandBuilder();

                deferStopCommand.WithName("deferstop");
                deferStopCommand.WithDescription("Defers the server from stopping for five minutes.");
                deferStopCommand.WithDefaultPermission(true);

                forceStartInstanceBuilder.WithName("forcestartinstance");
                forceStartInstanceBuilder.WithDescription("Admin only");
                forceStartInstanceBuilder.WithDefaultPermission(false);

                forceStopInstanceBuilder.WithName("forcestopinstance");
                forceStopInstanceBuilder.WithDescription("Admin only");
                forceStopInstanceBuilder.WithDefaultPermission(false);

                forceSyncBuilder.WithName("forcesync");
                forceSyncBuilder.WithDescription("Admin only");
                forceSyncBuilder.WithDefaultPermission(false);

                var deferStopCommandCmd = await Program._client.Rest.CreateGuildCommand(deferStopCommand.Build(), 795714783801245706);
                var forceStartInstance = await Program._client.Rest.CreateGuildCommand(forceStartInstanceBuilder.Build(), 795714783801245706);
                var forceStopInstance = await Program._client.Rest.CreateGuildCommand(forceStopInstanceBuilder.Build(), 795714783801245706);

                var forcesync = await Program._client.Rest.CreateGuildCommand(forceSyncBuilder.Build(), 795714783801245706);

                await forceStartInstance.ModifyCommandPermissions(new ApplicationCommandPermission[] {
                    new ApplicationCommandPermission(164890197237039104, ApplicationCommandPermissionTarget.User, true)
                });

                await forceStopInstance.ModifyCommandPermissions(new ApplicationCommandPermission[] {
                    new ApplicationCommandPermission(164890197237039104, ApplicationCommandPermissionTarget.User, true)
                });

                await forcesync.ModifyCommandPermissions(new ApplicationCommandPermission[] {
                    new ApplicationCommandPermission(164890197237039104, ApplicationCommandPermissionTarget.User, true)
                });

                if (guild.GetTextChannel(BotConfig.GetCachedConfig().Discord.MessageChannelId) != null)
                {
                    var chan = guild.GetTextChannel(BotConfig.GetCachedConfig().Discord.MessageChannelId);
                    if(BotConfig.GetCachedConfig().InternalMessageId == 0 || await chan.GetMessageAsync(BotConfig.GetCachedConfig().InternalMessageId) == null)
                    {
                        var comps = new ComponentBuilder();
                        comps.WithButton("Refresh Server", "STARTBOT_REFRESH", emote: new Emoji("🔄"));
                        var msg = await chan.SendMessageAsync(embed: Embeds.ServerStopped(), components: comps.Build());

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
            ThreadPool.QueueUserWorkItem(async delegate
            {
                await new WebServe().Run();
            });
            /*
             * Stop the server in case the bot crashed.
             */
            Console.WriteLine("Attempting server start.");
            try
            {
                var stopRequest = new StopInstancesRequest();
                var request = new StopInstancesRequest();
                request.InstanceIds = new List<string>()
                        {
                            BotConfig.GetCachedConfig().Aws.EC2InstanceId
                        };
                request.Force = false;
                var response = await ServerStateManager.Instance().client.StopInstancesAsync(request);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static ReactEventHandler _reh = new ReactEventHandler();

        private async Task HandleBotReactions(SocketMessageComponent smc)
        {
            await _reh.Handle(smc);
        }
        public void RegisterEvents()
        {
            _client.Ready += Start;
            //_client.ReactionAdded += HandleBotReactions;
            _client.InteractionCreated += async (inter) =>
            {
                if (inter is SocketMessageComponent smc)
                {
                    if (smc.Data.CustomId == "STARTBOT_REFRESH")
                    {
                        await HandleBotReactions(smc);
                        await smc.DeferAsync();
                    }
                }
                if(inter is SocketSlashCommand cmd)
                {
                    if(cmd.CommandName == "deferstop")
                    {
                        var status = await ServerStateManager.Instance().GetState();

                        if(status.InstanceState.Code == 16)
                        {
                            if(ServerStateManager.Instance().StopDeferred)
                            {
                                await cmd.RespondAsync("The stopping of the server has already been deferred.", ephemeral: false);
                            }
                            else
                            {
                                ServerStateManager.Instance().StopDeferred = true;
                                await cmd.RespondAsync("The stopping of the server is now deferred for an extra five minutes.", ephemeral: false);
                            }
                        }
                        else
                        {
                            await cmd.RespondAsync("The server is not running at the moment.", ephemeral: true);
                        }
                    }
                    if(cmd.CommandName == "forcestartinstance")
                    {
                        var request = new StartInstancesRequest();
                        request.InstanceIds = new List<string>()
                        {
                            BotConfig.GetCachedConfig().Aws.EC2InstanceId
                        };
                        var response = await ServerStateManager.Instance().client.StartInstancesAsync(request);
                        await cmd.RespondAsync(response.ToString(), ephemeral: true);
                    }
                    if (cmd.CommandName == "forcestopinstance")
                    {
                        var request = new StopInstancesRequest();
                        request.InstanceIds = new List<string>()
                        {
                            BotConfig.GetCachedConfig().Aws.EC2InstanceId
                        };
                        var response = await ServerStateManager.Instance().client.StopInstancesAsync(request);
                        await cmd.RespondAsync(response.ToString(), ephemeral: true);
                    }
                }
            };
        }

        private Task _client_Log(LogMessage l)
        {
            Console.WriteLine(l);
            return Task.CompletedTask;
        }
    }
}
