using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StartBot
{

    public class LangConfig
    {
        public string EmbedTitle { get; set; }
        public string EmbedServerStopped { get; set; }
        public string EmbedStartingServer { get; set; }
        public string EmbedServerStarted { get; set; }
        public string EmbedServerStopping { get; set; }
        public string EmbedImage { get; set; }
        public string EmbedFooter { get; set; }
    }

    public class MinecraftConfig
    {
        public string MinecraftServerIP { get; set; }

        public ushort MinecraftServerPort { get; set; }
    }

    public class AWSConfig
    {
        public string EC2InstanceId { get; set; }
        public string EC2AccessKey { get; set; }

        public string EC2AccessSecret { get; set; }
        public ushort MaxInstanceStartAttempts { get; set; }

        public string Region { get; set; }
    }

    public class DiscordConfig
    {
        public string BotToken { get; set; }
        public ulong GuildId { get; set; }
        public ulong MessageChannelId { get; set; }
    }

    public class BotConfig
    { 
        
        public LangConfig Lang { get; set; }
        public AWSConfig Aws { get; set; }
        public DiscordConfig Discord { get; set; }

        public MinecraftConfig Minecraft { get; set; }
        public ulong StopServerDelay { get; set; }
        public ulong PollingDelay { get; set; }

        public ulong InternalMessageId { get; set; }


        private static BotConfig? configCache;

        // so we don't have to load the config every time a value needs to be read from it
        public static BotConfig GetCachedConfig()
        {
            if (configCache == null)
            {
                LoadConfig();
            }
            return configCache;
        }



        //loads config, forcing the cache to update
        public static BotConfig LoadConfig()
        {
            string fileContents = File.ReadAllText("config.yml");
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var p = deserializer.Deserialize<BotConfig>(fileContents);
            configCache = p;
            return p;
        }

        public static void SaveConfig(BotConfig cfg)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yml = serializer.Serialize(cfg);
            File.WriteAllText("config.yml", yml);
            LoadConfig(); // update the cache
        }

    }

}
