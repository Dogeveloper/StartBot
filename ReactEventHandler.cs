
using Amazon.EC2;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartBot
{
    public class ReactEventHandler
    {
        private bool debounce = false;
        public async Task Handle(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel chan, SocketReaction r)
        {
            if (r.UserId == Program._client.CurrentUser.Id) { return; }
            if(r.MessageId == BotConfig.GetCachedConfig().InternalMessageId)
            {
                if(r.Emote.Name == "🔄")
                {
                    var actualMessage = await chan.GetMessageAsync(r.MessageId);
                    await actualMessage.RemoveReactionAsync(r.Emote, r.UserId);

                    if(!debounce && !ServerStateManager.Instance().IsWorking) // ignore attempts to spam the button
                    {
                        debounce = true;
                        var status = await ServerStateManager.Instance().GetState();
                        if(status.InstanceState.Code == 80)
                        {
                                ThreadPool.QueueUserWorkItem(async delegate
                                {
                                    try
                                    {
                                        await ServerStateManager.Instance().StartServer();
                                    }
                                    catch (AmazonEC2Exception e)
                                    {
                                        Console.WriteLine("AWS threw an error.");
                                        Console.Write(e.Message);
                                        ServerStateManager.Instance().IsWorking = false;
                                        await Embeds.EditMessage(Embeds.ServerStopped("The server could not be started previously due to an error. Please wait a bit and try again, or ask an admin if this persists. This mainly happens after starting a server when it's just been stopped."));
                                    }

                                });
                           
                        }
                        else
                        {
                            Console.WriteLine("Rejecting refresh as server is not stopped.");
                            if (Program._client.GetUser(r.UserId) != null)
                            {
                                var usr = await chan.GetUserAsync(r.UserId);
                                await usr.SendMessageAsync(embed: Embeds.Error("The server must be stopped for the refresh button to work."));
                            }
                        }
                        debounce = false;
                    }
                }
            }
        }
    }
}
