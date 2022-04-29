using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TraceLd.MineStatSharp;

namespace StartBot
{
    public class ServerStateManager
    {

        public AmazonEC2Client client;
        private ServerStateManager()
        {
            client = new AmazonEC2Client(new BasicAWSCredentials(BotConfig.GetCachedConfig().Aws.EC2AccessKey, BotConfig.GetCachedConfig().Aws.EC2AccessSecret), RegionEndpoint.GetBySystemName(BotConfig.GetCachedConfig().Aws.Region));
        }

        private static readonly ServerStateManager _singleton = new ServerStateManager();

        public bool IsWorking = false;

        public bool StopDeferred = false;


        public static ServerStateManager Instance()
        {
            return _singleton;
        }

        public async Task<InstanceStatus> GetState()
        {
            var request = new DescribeInstanceStatusRequest();
            request.InstanceIds = new List<string>()
            {
                BotConfig.GetCachedConfig().Aws.EC2InstanceId
            };
            request.IncludeAllInstances = true; // show instances that aren't running as well
            var response = await client.DescribeInstanceStatusAsync(request);
            InstanceStatus status = null;
            response.InstanceStatuses.ForEach(i => // unpack the response into an instance status
            {
                Console.WriteLine("checking state of instance " + i.InstanceId);
                status = i;
            });
            return status;
        }

        private async Task<bool> AwaitServerStart()
        {
            ushort attempts = 0;
            while (attempts < BotConfig.GetCachedConfig().Aws.MaxInstanceStartAttempts)
            {
                await Task.Delay((int) BotConfig.GetCachedConfig().PollingDelay);
                var state = await this.GetState();
                if (state.InstanceState.Code == 16) // 16 = instance started
                {
                    await Program.ModLog("MC AWS Server Started", "");
                    return true;
                }
                attempts++;
            }
            return false;
        }
        
        public async Task StopServer()
        {
            while(true)
            {
                await Task.Delay((int) BotConfig.GetCachedConfig().PollingDelay);
                MineStat ms = new MineStat(BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP, BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort);
                Console.WriteLine("Current player count: " + ms.CurrentPlayers);
                if (ms.CurrentPlayers == "0")
                {
                    Console.WriteLine("Server player is count is zero. Sleeping, and checking again.");
                    await Task.Delay((int)BotConfig.GetCachedConfig().StopServerDelay);
                    if (this.StopDeferred)
                    {
                        this.StopDeferred = false;
                        continue;
                    }
                    ms = new MineStat(BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP, BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort);
                    if (ms.ServerUp && ms.CurrentPlayers == "0")
                    {
                        await Program.ModLog("MC Server Stopping", "");
                        Console.WriteLine("Player count is still zero. Stopping.");
                        await Embeds.EditMessage(Embeds.ServerStopping());
                        var stopRequest = new StopInstancesRequest();
                        var request = new StopInstancesRequest();
                        request.InstanceIds = new List<string>()
                        {
                            BotConfig.GetCachedConfig().Aws.EC2InstanceId
                        };
                        request.Force = false;
                        var response = await client.StopInstancesAsync(request);
                        ushort pollCounter = 0;
                        while(pollCounter < BotConfig.GetCachedConfig().Aws.MaxInstanceStartAttempts)
                        {
                            pollCounter++;
                            await Task.Delay(5000);
                            var serverState = await this.GetState();
                            if(serverState.InstanceState.Code == 80)
                            {
                                break;
                            }
                        }
                        await Program.ModLog("MC Server Stopped", "");
                        await Embeds.EditMessage(Embeds.ServerStopped());
                        break;
                    }
                }
            }
            IsWorking = false;
        }

        public async Task StartServer()
        {
            Console.WriteLine("Starting server.");
            await Program.ModLog("MC Server Starting", "");
            IsWorking = true;
            await Embeds.EditMessage(Embeds.ServerStarting());
            var request = new StartInstancesRequest();
            request.InstanceIds = new List<string>()
            {
                BotConfig.GetCachedConfig().Aws.EC2InstanceId
            };
            var response = await client.StartInstancesAsync(request);
            bool suc = await AwaitServerStart();
            if(suc)
            {
                while(true)
                {
                    await Task.Delay((int) BotConfig.GetCachedConfig().PollingDelay);
                    try
                    {
                        MineStat ms = new MineStat(BotConfig.GetCachedConfig().Minecraft.MinecraftServerIP, BotConfig.GetCachedConfig().Minecraft.MinecraftServerPort);
                        if(ms.ServerUp)
                        {
                            Console.WriteLine("MC Server up.");
                            await Program.ModLog("MC Server is UP", "");
                            break;
                        }
                        Console.WriteLine("MC Server is not up.");
                    }
                    catch
                    {
                        Console.WriteLine("Polling Minecraft server to see if it is online.. resulted in error.");
                    }
                }
                await Embeds.EditMessage(Embeds.ServerStarted());
                await StopServer();
            }
            else
            {
                Console.WriteLine("Server start failed due to maxing out maxInstanceStartAttempts.");
                return;
            }
        }
    }
}
