# StartBot

Hosting on AWS can be expensive especially when running a server 24/7. StartBot optimizes this by automatically stopping your Minecraft server when nobody is playing, and your players can start the server again on Discord when they want to play with the click of a single reaction. Unlike free hosts, there is no queue to wait in, and CPU capacity will not be crammed in with a bunch of other servers.

# Setup

Create a config file in the working directory for the bot, `config.yml`. You will need to create a Discord bot, and have an AWS server that is running Minecraft. The bot should work with any panel solution, as long as it can handle starting the Minecraft server automatically when the AWS instance starts.

```yml
#This defines the time between when the bot checks for players on your server and the succeding check, in milliseconds. Longer values are generally more lenient to players.
stopServerDelay: 300000
#How often the bot will query AWS and Minecraft when checking for various conditions.
pollingDelay: 5000
minecraft:
    #Do not include the port in this value. Instead, use minecraftServerPort.
    minecraftServerIP: 
    #Even if your server is running on port 25565, make sure to fill this in.
    minecraftServerPort: 
aws:
    eC2AccessKey: 
    eC2AccessSecret: 
    eC2InstanceId: 
    #How many times the bot will poll AWS for a server start before giving up. If it gives up due to a server problem or this being too low, you'll get an error and you will need to fix the issue.
    maxInstanceStartAttempts: 70
    #Change this to the region your instance is in.
    region: us-west-2
discord:
    botToken: 
    #The channel players will use to start the server. The bot will automatically set this up for you, provided it has permision.
    messageChannelId: 
    #The guild the message channel is in.
    guildId: 
lang:
    #The title used for messages.
    embedTitle: 
    #URL to image used for messages. Optional
    embedImage: 
    embedFooter: The IP to connect is %ip%:%port%.
    embedServerStopped: The server is currently offline. Click the 'refresh' button below to start it.
    embedStartingServer: The server is currently starting.
    embedServerStarted: The server is currently online.
    embedServerStopping: The server is currently stopping.
#Don't modify unless you know what you are doing.
internalMessageId: 0
```

 Provided you configured everything correctly, the bot works out of the box. It will create a new message automatically in the channel you specified with an option for players to start the server.
 
 Players can start the server by clicking the "refresh" button, and it will start.
 
 ## Spot Instances
StartBot has been tested on a spot instance, and it should work normally even with spot interruptions. Provided you set the max bid to the on-demand price, interruptions will be very uncommon.
