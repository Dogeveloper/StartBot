
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
        public async Task Handle(SocketMessageComponent? smc, bool web = false)
        {
                    if(!debounce && !ServerStateManager.Instance().IsWorking) // ignore attempts to spam the button
                    {
                        debounce = true;
                        var status = await ServerStateManager.Instance().GetState();
                        if(status.InstanceState.Code == 80)
                        {
                            if (!web)
                            {
                                await Program.ModLog("Server Started From Discord", "Initated by " + smc.User.Id + " / " + smc.User.Mention);
                                Console.WriteLine("User ID" + smc.User.Id + " initated a server start.");
                            }
                            else
                    {
                        await Program.ModLog("Server Started From Web Portal", "");
                    }
                            
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
                            if (smc != null && Program._client.GetUser(smc.User.Id) != null)
                            {
                                var usr = await smc.Channel.GetUserAsync(smc.User.Id);
                                await usr.SendMessageAsync(embed: Embeds.Error("The server must be stopped for the refresh button to work."));
                            }
                        }
                        debounce = false;
                    }
                }
            }
        }