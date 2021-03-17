using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StartBot
{
    public class Embeds
    {
        private Embeds() { }
        public static Embed ServerStopped(string footer = "")
        {
            var builder = new EmbedBuilder();
            return builder.WithTitle(BotConfig.GetCachedConfig().Lang.EmbedTitle)
                .WithDescription(BotConfig.GetCachedConfig().Lang.EmbedServerStopped)
                .WithColor(Discord.Color.Red)
                .WithFooter(footer)
                .WithThumbnailUrl(BotConfig.GetCachedConfig().Lang.EmbedImage)
                .Build();
        }
        public static Embed ServerStarting()
        {
            var footer = new StringBuilder(BotConfig.GetCachedConfig().Lang.EmbedFooter);
            footer.Replace("%ip%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP);
            footer.Replace("%port%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort.ToString());
            var builder = new EmbedBuilder();
            return builder.WithTitle(BotConfig.GetCachedConfig().Lang.EmbedTitle)
                .WithDescription(BotConfig.GetCachedConfig().Lang.EmbedStartingServer)
                .WithColor(new Discord.Color(255, 255, 0)) //yellow
                .WithThumbnailUrl(BotConfig.GetCachedConfig().Lang.EmbedImage)
                .WithFooter(footer.ToString())
                .Build();
        }

        public static Embed ServerStarted()
        {
            var footer = new StringBuilder(BotConfig.GetCachedConfig().Lang.EmbedFooter);
            footer.Replace("%ip%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP);
            footer.Replace("%port%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort.ToString());
            var builder = new EmbedBuilder();
            return builder.WithTitle(BotConfig.GetCachedConfig().Lang.EmbedTitle)
                .WithDescription(BotConfig.GetCachedConfig().Lang.EmbedServerStarted)
                .WithColor(Discord.Color.Green)
                .WithFooter(footer.ToString())
                .WithThumbnailUrl(BotConfig.GetCachedConfig().Lang.EmbedImage)
                .Build();
        }

        public static Embed ServerStopping()
        {
            var builder = new EmbedBuilder();
            var footer = new StringBuilder(BotConfig.GetCachedConfig().Lang.EmbedFooter);
            footer.Replace("%ip%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP);
            footer.Replace("%port%", BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort.ToString());
            return builder.WithTitle(BotConfig.GetCachedConfig().Lang.EmbedTitle)
                .WithDescription(BotConfig.GetCachedConfig().Lang.EmbedServerStopping)
                .WithColor(Discord.Color.Orange)
                .WithFooter(footer.ToString())
                .WithThumbnailUrl(BotConfig.GetCachedConfig().Lang.EmbedImage)
                .Build();
        }

        public static Embed Error(string message)
        {
            var builder = new EmbedBuilder();
            return builder.WithTitle("Error")
                .WithDescription(message)
                .WithColor(Discord.Color.Red)
                .WithThumbnailUrl(BotConfig.GetCachedConfig().Lang.EmbedImage)
                .Build();
        }

        public static async Task EditMessage(Embed e)
        {
            var client = Program._client;
            var guild = client.GetGuild(BotConfig.GetCachedConfig().Discord.GuildId);
            var channel = guild.GetTextChannel(BotConfig.GetCachedConfig().Discord.MessageChannelId);
            var message = await (channel.GetMessageAsync(BotConfig.GetCachedConfig().InternalMessageId)) as RestUserMessage;
            await message.ModifyAsync(m =>
            {
                m.Embed = new Optional<Embed>(e);
            });
        }
    }
}
