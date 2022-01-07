### Introduction
Currently space credits and the economy system itself don't really have a use on many servers. And equally many have issues where PVP is not really a thing although they advertise it. So this plugin attempts to solve both of that.

Introducing: GPS Roulette. Players are free to purchase GPS coordinates of random players locations that meet certain criteria. Compared to the bounty contracts in the economy system which follows the players on every step this purchase is just a snapshot of the current situation.

So the unlike the bounty contracts you cannot abuse it to non stop follow a player around. Once he jumped or was killed the GPS will be obsolete. So no trolling whatsoever. 

### Player Commands
- !gps buy random
 - Provides a random GPS coord in exchange for credits.
- !gps buy online
 - Provides a random GPS coord of an online player in exchange for credits.
- !gps buy inactive
 - Provides a random GPS coord of an inactive player in exchange for credits.
- !gps buy npc
 - Provides a random GPS coord of an NPC station in exchange for credits.
- !gps list commands
 - Lists buy commands that are enabled.
- !gps list chances
 - Lists chances of getting certain commands randomly.
- !gps help
 - Displays how GPS Roulette works.

### Moderator Commands
- !gps list all
 - Lists all identities whose GPS can currently be bought.
- !gps list random
 - Lists all identities whose GPS can currently be bought via !gps buy random. (NPC Filters are applied)
- !gps list online
 - Lists only online identities whose GPS can currently be bought.
- !gps list inactive
 - Lists only inactive identities whose GPS can currently be bought.
- !gps list npc
 - Lists only NPC identities whose GPS can currently be bought.

### How it works / Configuration
When a player enters the !gps buy random command for example all players will be evaluated and filtered. For example you can configure that they have to have a minimum amount of PCU in the world, if faction members are filtered, when they count as inactive etc. 

Also the price can be configured individually. random, npc, inactive and online can have different prices. Cooldowns however apply to all of them. By default a player can only buy GPS every 30 minutes. You can alter that or turn it off completely if you like. 

Of the list of valid players one will randomly be choosen. 
- If the player is online his correct location will be taken.
- If the player is offline the location of his body will be taken for example cryo chamber.
-- If not available last death location it is. Because when a player dies upon disconnect thats the last known location of them.
- If the player is an NPC one random NPC station of that player is selected.

If you dont want an spot on selection you can also add an offset. for example 15 km away from that player.

If the player whose GPS was bought is online there is an option to notify them about their GPS being bought.  

### Github
[https://github.com/LordTylus/SE-Torch-Plugin-ALE-GpsRoulette](https://github.com/LordTylus/SE-Torch-Plugin-ALE-GpsRoulette)