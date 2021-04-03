using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Utils;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    [Category("gps")]
    public class GpsRouletteCommands : CommandModule {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GpsRoulettePlugin Plugin => (GpsRoulettePlugin) Context.Plugin;

        [Command("help", "List active commands to buy GPS.")]
        [Permission(MyPromoteLevel.None)]
        public void Help() {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("The GPS-Roulette plugin allows you to purchase gps locations of other players in exchange for space credits to improve PVP on certain servers and make the economy system more useful.");
            sb.AppendLine();
            sb.AppendLine("There are different commands you can use to purchase said gps. You can find them by typing '!gps list commands' in chat or in the list below.");
            sb.AppendLine();
            sb.AppendLine("To purchase a players location the following criteria must be met:");

            var lastOnlineMinutes = Plugin.Config.LastOnlineMinutes;

            if (lastOnlineMinutes <= 0)
                sb.AppendLine("- The player must be online.");
            else
                sb.AppendLine("- The player must be online or offline for less then "+ lastOnlineMinutes.ToString("#,##0") + " minutes.");

            var offlineHours = Plugin.Config.OfflineLongerThanHours;

            if(offlineHours > 0)
                sb.AppendLine("-- Or the player is inactive (offline) for more than "+ offlineHours.ToString("#,##0") + " hours.");
            
            if (!Plugin.Config.IncludePlayersWithoutFaction)
                sb.AppendLine("- The player must be in a faction.");

            var minPCU = Plugin.Config.MinPCUToBeFound;
            if(minPCU > 0)
                sb.AppendLine("- The player must have at least " + minPCU.ToString("#,##0") + " PCU.");

            var minPCUToBuy = Plugin.Config.MinPCUToBuy;
            var minOnlineTime = Plugin.Config.MinOnlineMinutesToBuy;
            var minPlayersOnline = Plugin.Config.MinPlayerOnlineToBuy;
            var mustBeInFaction = Plugin.Config.MustBeInFactionToBuy;
            var cooldown = Plugin.Config.CooldownMinutes;

            if (minPCUToBuy > 0 || minOnlineTime > 0 || minPlayersOnline > 0 || mustBeInFaction || cooldown > 0) {

                sb.AppendLine();
                sb.AppendLine("To be able to purchase gps you must meet the following citeria:");

                if (minPCUToBuy > 0)
                    sb.AppendLine("- You must have at least " + minPCUToBuy.ToString("#,##0") + " PCU.");

                if (minOnlineTime > 0)
                    sb.AppendLine("- You must be online for at least " + minOnlineTime.ToString("#,##0") + " minutes.");

                if(cooldown > 0)
                    sb.AppendLine("- You must not have bought a GPS within the last " + cooldown.ToString("#,##0") + " minutes.");

                if (minPlayersOnline > 0)
                    sb.AppendLine("- At least " + minPlayersOnline.ToString("#,##0") + " players must be online to buy random/online gps.");

                if(mustBeInFaction)
                    sb.AppendLine("- Be part of a faction.");
            }

            sb.AppendLine();

            sb.AppendLine("You can purchase the following locations:");
            sb.AppendLine("- If the player is online you get their location.");
            sb.AppendLine("- If the player is offline you get the location of their body.");
            sb.AppendLine("- If no body is found you get the location they last died at (on logout).");
            sb.AppendLine("- If the player is an NPC you get the location of their NPC station.");

            sb.AppendLine();

            sb.AppendLine("Additional information:");

            var notified = Plugin.Config.NotifySoldPlayer;

            if(notified)
                sb.AppendLine("- If your location was bought you will be notified on screen when online.");
            else
                sb.AppendLine("- If your location was bought you will not get any notifitication about it.");

            var offset = Plugin.Config.GpsOffsetFromPlayerKm;

            if(offset == 0)
                sb.AppendLine("- Bought locations will minimum be directly on target.");
            else
                sb.AppendLine("- Bought locations will minimum be about " + offset.ToString("#,##0")+" km away from the target.");

            var offsetMax = Plugin.Config.GpsOffsetFromPlayerKmMax;

            if (offsetMax < offset)
                offsetMax = offset;

            if (offsetMax == 0)
                sb.AppendLine("- Bought locations will maximum be directly on target.");
            else
                sb.AppendLine("- Bought locations will maximum be about " + offsetMax.ToString("#,##0") + " km away from the target.");


            var filterFaction = Plugin.Config.FilterFactionMembers;

            if(filterFaction)
                sb.AppendLine("- Locations of faction members will be ignored.");
            else
                sb.AppendLine("- You can also randomly purchase the location of a faction member.");

            sb.AppendLine();

            sb.AppendLine("Currently active commands:");
            AddCommandsToSb(sb,"- ");

            if (Context.Player == null) {

                Context.Respond($"GPS Roulette help");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("GPS Roulette help", "", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("list commands", "List active commands to buy GPS.")]
        [Permission(MyPromoteLevel.None)]
        public void ListCommands() {

            StringBuilder sb = new StringBuilder();

            AddCommandsToSb(sb);

            if (Context.Player == null) {

                Context.Respond($"Active buy commands");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Active buy commands", "", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private void AddCommandsToSb(StringBuilder sb, string prefix = "") {

            bool shouldShowAdjustedPrices = false;
            MyIdentity identity = null;

            if (Plugin.Config.UseDynamicPrices) {

                var player = Context.Player;

                if (player != null) {

                    identity = PlayerUtils.GetIdentityById(player.IdentityId);

                    if (FactionUtils.GetPlayerFaction(identity.IdentityId) != null)
                        shouldShowAdjustedPrices = true;
                }

                sb.AppendLine();

                sb.AppendLine("Prices change dynamically relative to the number of members your faction has.");
                sb.AppendLine("Actual Price = baseprice + baseprice * multiplier * (Number of Factionmembers - 1)");
                sb.AppendLine("Current Multiplier is " + Plugin.Config.DynamicPriceMultiplier.ToString("#,##0.00"));

                sb.AppendLine();
            }

            var price = Plugin.Config.PriceCreditsRandom;
            if (price >= 0) {

                sb.AppendLine(prefix + "!gps buy random -- for " + price.ToString("#,##0") + " SC (Baseprice)");

                if(shouldShowAdjustedPrices)
                    sb.AppendLine(prefix + "   You would pay "+ GetAdjustedPriceForPlayer(price, identity).ToString("#,##0") + " SC");
            }

            price = Plugin.Config.PriceCreditsOnline;
            if (price >= 0) {
                
                sb.AppendLine(prefix + "!gps buy online -- for " + price.ToString("#,##0") + " SC (Baseprice)");
                
                if (shouldShowAdjustedPrices)
                    sb.AppendLine(prefix + "   You would pay " + GetAdjustedPriceForPlayer(price, identity).ToString("#,##0") + " SC");
            }

            price = Plugin.Config.PriceCreditsInactive;
            if (price >= 0) {
                
                sb.AppendLine(prefix + "!gps buy inactive -- for " + price.ToString("#,##0") + " SC (Baseprice)");
                
                if (shouldShowAdjustedPrices)
                    sb.AppendLine(prefix + "   You would pay " + GetAdjustedPriceForPlayer(price, identity).ToString("#,##0") + " SC");
            }

            price = Plugin.Config.PriceCreditsNPC;
            if (price >= 0) {

                sb.AppendLine(prefix + "!gps buy npc -- for " + price.ToString("#,##0") + " SC (Baseprice)");

                if (shouldShowAdjustedPrices)
                    sb.AppendLine(prefix + "   You would pay " + GetAdjustedPriceForPlayer(price, identity).ToString("#,##0") + " SC");
            }

            if (Plugin.Config.UseDynamicPrices) {
                sb.AppendLine();
                sb.AppendLine("Keep in mind: These prices change relative to the number of members your faction has.");
            }
        }

        [Command("buy random", "Provides a random GPS coord in exchange for credits.")]
        [Permission(MyPromoteLevel.None)]
        public void BuyRandom() {

            var price = Plugin.Config.PriceCreditsRandom;

            BuyInternal(price, PurchaseMode.RANDOM);
        }

        [Command("buy online", "Provides a random GPS coord of an online player in exchange for credits.")]
        [Permission(MyPromoteLevel.None)]
        public void BuyOnline() {

            var price = Plugin.Config.PriceCreditsOnline;

            BuyInternal(price, PurchaseMode.ONLINE);
        }

        [Command("buy inactive", "Provides a random GPS coord of an inactive player in exchange for credits.")]
        [Permission(MyPromoteLevel.None)]
        public void BuyInactive() {

            var price = Plugin.Config.PriceCreditsInactive;

            BuyInternal(price, PurchaseMode.INACTIVE);
        }

        [Command("buy npc", "Provides a random GPS coord of an NPC station in exchange for credits.")]
        [Permission(MyPromoteLevel.None)]
        public void BuyNpc() {

            var price = Plugin.Config.PriceCreditsNPC;

            BuyInternal(price, PurchaseMode.NPC);
        }

        private void BuyInternal(long price, PurchaseMode mode) {

            if(Context.Player == null) {
                Context.Respond("Only an actual player can buy GPS!");
                return;
            }
                
            if(price < 0) {
                Context.Respond("This command was disabled in the settings. Use '!gps list commands' to see which commands are active!");
                return;
            }

            var player = Context.Player;
            var identity = PlayerUtils.GetIdentityById(player.IdentityId);

            price = GetAdjustedPriceForPlayer(price, identity);

            if (Plugin.Config.MustBeInFactionToBuy) {

                var faction = FactionUtils.GetPlayerFaction(identity.IdentityId);

                if(faction == null) {

                    Context.Respond("You must be part of a Faction to buy GPS!");
                    return;
                }
            }

            if (mode == PurchaseMode.ONLINE || mode == PurchaseMode.RANDOM) {

                var minPlayersOnline = Plugin.Config.MinPlayerOnlineToBuy;

                int onlineCount = MySession.Static.Players.GetOnlinePlayerCount();

                if(onlineCount < minPlayersOnline) {

                    Context.Respond("For Online/Random gps at least "+ minPlayersOnline + " players must be online!");
                    return;
                }
            }

            var minOnlineTime = Plugin.Config.MinOnlineMinutesToBuy;

            if(minOnlineTime > 0) {

                var lastSeen = PlayerUtils.GetLastSeenDate(identity);
                var minOnlineDate = DateTime.Now.AddMinutes(-Plugin.Config.MinOnlineMinutesToBuy);

                if (lastSeen > minOnlineDate) {

                    int differenceSeconds = (int) lastSeen.Subtract(minOnlineDate).TotalSeconds;

                    int minutes = (differenceSeconds / 60);
                    int seconds = differenceSeconds % 60;

                    Context.Respond("You are not online for long enough! You must be online for at least " + minutes.ToString("00") + ":" + seconds.ToString("00") + " more minutes!");

                    return;
                }
            }

            var minPCUToBuy = Plugin.Config.MinPCUToBuy;

            if(minPCUToBuy > 0) {

                var pcuBuilt = identity.BlockLimits.PCUBuilt;
                var neededPcu = Plugin.Config.MinPCUToBuy;

                if (neededPcu > pcuBuilt) {

                    Context.Respond("You dont have enough PCU to buy! You need at least " + (neededPcu - pcuBuilt) + " more!");

                    return;
                }
            }
            
            var cooldownManager = Plugin.CooldownManager;
            var steamId = new SteamIdCooldownKey(player.SteamUserId);
            var cooldownCommand = "buy";

            long currentBalance = MyBankingSystem.GetBalance(player.IdentityId);

            if (currentBalance < price) {
                Context.Respond("You dont have enough credits to effort a GPS! You need at least " + price.ToString("#,##0") + " SC.");
                return;
            }

            if (!cooldownManager.CheckCooldown(steamId, cooldownCommand, out long remainingSeconds)) {
                Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                return;
            } 

            var buyables = FindFilteredBuyablesForPlayer(mode);

            if (buyables.Count == 0) {
                Context.Respond("Currently there is no GPS available for purchase. Please try again later!");
                return;
            }

            if (!CheckConformation(steamId, mode, price))
                return;

            if (BuyRandomFromDict(buyables)) {

                var config = Plugin.Config;
                var cooldownMs = config.CooldownMinutes * 60 * 1000L;

                MyBankingSystem.ChangeBalance(player.IdentityId, -price);

                cooldownManager.StartCooldown(steamId, cooldownCommand, cooldownMs);

                Context.Respond("Purchase successful!");

            } else {

                Context.Respond("The location of the selected player could not be retrieved. The purchase was cancelled. Please try again.");
            }
        }

        private long GetAdjustedPriceForPlayer(long price, MyIdentity identity) {

            if (!Plugin.Config.UseDynamicPrices)
                return price;

            var faction = FactionUtils.GetPlayerFaction(identity.IdentityId);

            if (faction == null)
                return price;

            int numberOfMembers = faction.Members.Count;

            float priceMultiplier = Plugin.Config.DynamicPriceMultiplier;

            return price + (long) Math.Round(price * (numberOfMembers - 1) * priceMultiplier);
        }

        private bool BuyRandomFromDict(Dictionary<long, List<PurchaseMode>> buyables) {

            List<long> buyablesAsList = new List<long>(buyables.Keys);

            var config = Plugin.Config;
            MyGps position = null;

            long identityId = Context.Player.IdentityId;
            long foundIdentity = 0L;

            /* Try 5 times to make sure the player gets something. If we wont find anything after 5 times we stop */
            for (int i = 0; i < 5; i++) {

                buyablesAsList.ShuffleList();
               
                foundIdentity = buyablesAsList.First();

                position = FindPositionOfPlayer(foundIdentity);

                if (position != null)
                    break;
            }

            if (position == null)
                return false;

            SendGps(position, identityId);

            var location = position.Coords;
            var otherPlayer = PlayerUtils.GetIdentityById(foundIdentity);

            if(otherPlayer != null)
                Log.Info("Player " + Context.Player.DisplayName + " bought GPS Location X: " + location.X + " Y: " + location.Y + " Z: " + location.Z + " of Player " + otherPlayer.DisplayName);

            if (config.NotifySoldPlayer) {

                var message = "Watch out! Someone bought your current location. They will be here soon!";

                MyVisualScriptLogicProvider.ShowNotification(
                    message,10000, MyFontEnum.White, foundIdentity);

                MyVisualScriptLogicProvider.SendChatMessage(
                    message, Plugin.Torch.Config.ChatName, foundIdentity, MyFontEnum.Red);
            }

            return true;
        }

        private MyGps FindPositionOfPlayer(long foundIdentity) {

            var identity = PlayerUtils.GetIdentityById(foundIdentity);

            if (identity == null) {
                Log.Error("Could not find Identity to ID #"+foundIdentity);
                return null;
            }

            var factionCollection = MySession.Static.Factions;
            var playerCollection = MySession.Static.Players;

            if (playerCollection.IdentityIsNpc(foundIdentity)) {

                var faction = factionCollection.GetPlayerFaction(foundIdentity);

                if (faction == null) {
                    Log.Error("NPC Identity "+ identity.DisplayName + " has no faction!");
                    return null;
                }

                var stations = faction.Stations;

                if (stations.Count == 0) {
                    Log.Error("NPC Faction "+ faction.Tag + " has no stations!");
                    return null;
                }

                var stationsList = new List<MyStation>(stations);
                stationsList.ShuffleList();

                var station = stations.First();

                return CreateGps(station.Position, identity);
            }

            var character = identity.Character;

            if (character == null) {

                if (!playerCollection.IsPlayerOnline(foundIdentity)) {

                    /* If player is not online look for him in beds cryopods etc. */
                    foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>().ToList()) {
                        foreach (var controller in grid.GetFatBlocks<MyShipController>()) {

                            var pilot = controller.Pilot;

                            if (pilot != null && pilot.GetPlayerIdentityId() == foundIdentity) 
                                return CreateGps(controller.PositionComp.GetPosition(), identity);
                        }
                    }
                }

                if (identity.LastDeathPosition != null)
                    return CreateGps(identity.LastDeathPosition.Value, identity);

                Log.Error("Player "+identity.DisplayName+" has no last death location!");

                return null;
            }

            return CreateGps(character.PositionComp.GetPosition(), identity);
        }

        private MyGps CreateGps(Vector3D location, MyIdentity identity) {

            var offset = Plugin.Config.GpsOffsetFromPlayerKm;
            var offsetMax = Plugin.Config.GpsOffsetFromPlayerKmMax;

            if (offsetMax < offset)
                offsetMax = offset;

            if (offset > 0) {

                double distance = offset * 1000.0;
                double distanceMax = offsetMax * 1000.0;

                location = FindRandomPosition(location, distance, distanceMax);
            }

            MyGps gps = new MyGps {
                Coords = location,
                Name = "Location of " + identity.DisplayName+" #" + DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                DisplayName = "Location of " + identity.DisplayName,
                Description = "Bought via GPS Roulette plugin",
                GPSColor = Color.Cyan,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan?(),
                AlwaysVisible = true
            };
            gps.UpdateHash();

            return gps;
        }

        private static Vector3D FindRandomPosition(Vector3D origin, double min, double max) {

            double randomX = MyUtils.GetRandomDouble(-1, 1);
            double randomY = MyUtils.GetRandomDouble(-1, 1);
            double randomZ = MyUtils.GetRandomDouble(-1, 1);

            Vector3D random = new Vector3D(randomX, randomY, randomZ);

            double distanceToOrigin = random.Length();

            double distance = MyUtils.GetRandomDouble(min, max);

            return origin + (random * (distance / distanceToOrigin));
        }

        private bool SendGps(MyGps gps, long playerId) {

            MyGpsCollection gpsCollection = (MyGpsCollection) MyAPIGateway.Session?.GPS;

            if (gpsCollection == null)
                return false;

            bool playSound = true;
            long entityId = 0L;

            gpsCollection.SendAddGps(playerId, ref gps, entityId, playSound);
            
            return true;
        }

        private bool CheckConformation(ICooldownKey cooldownKey, PurchaseMode mode, long price) {

            var cooldownManager = Plugin.ConfirmationManager;

            var commandKey = mode.ToString() + price;

            if (!cooldownManager.CheckCooldown(cooldownKey, commandKey, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            Context.Respond("Are you sure you want to buy a gps for '"+mode+"' for "+ price.ToString("#,##0") + " SC? Make sure you read the information in !gps help. Enter the command again within 30 seconds to confirm!");

            cooldownManager.StartCooldown(cooldownKey, commandKey, 30 * 1000);

            return false;
        }

        [Command("list random", "Lists all identities whose GPS can currently be bought.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void ListRandom() {
            ListInternal(PurchaseMode.RANDOM);
        }

        [Command("list online", "Lists only online identities whose GPS can currently be bought.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void ListOnline() {
            ListInternal(PurchaseMode.ONLINE);
        }

        [Command("list inactive", "Lists only inactive identities whose GPS can currently be bought.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void ListInactive() {
            ListInternal(PurchaseMode.INACTIVE);
        }

        [Command("list npc", "Lists only NPC identities whose GPS can currently be bought.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void ListNpc() {
            ListInternal(PurchaseMode.NPC);
        }

        private void ListInternal(PurchaseMode mode) {

            var buyables = FindFilteredBuyables(mode);

            List<string> lines = new List<string>();

            foreach(var entry in buyables) {

                long identityId = entry.Key;
                List<PurchaseMode> modes = entry.Value;

                IMyFaction faction = FactionUtils.GetPlayerFaction(identityId);

                string factionString = "";
                if (faction != null)
                    factionString = "[" + faction.Tag + "]";

                string name = PlayerUtils.GetPlayerNameById(identityId) + " " + factionString;
                string modesStr = string.Join(", ", modes);

                lines.Add(name + " -- " + modesStr);
            }

            lines.Sort();

            StringBuilder sb = new StringBuilder();

            foreach (var line in lines)
                sb.AppendLine(line);

            if (Context.Player == null) {

                Context.Respond($"Buyable GPS for "+ mode);
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Buyable GPS", "For "+ mode, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private Dictionary<long, List<PurchaseMode>> FindBuyables() {

            var factionCollection = MySession.Static.Factions;
            var playerCollection = MySession.Static.Players;
            var identities = playerCollection.GetAllIdentities();
            var result = new Dictionary<long, List<PurchaseMode>>();

            var config = Plugin.Config;

            foreach(var identity in identities) {

                long identityId = identity.IdentityId;

                MyFaction faction = factionCollection.GetPlayerFaction(identityId); 

                /* If we dont want to include factionless players we have to filter here now */
                if (!config.IncludePlayersWithoutFaction && faction == null) 
                    continue;

                bool isNpc = playerCollection.IdentityIsNpc(identityId);
                bool mustCheckPcu = !isNpc || config.MinPCUAlsoForNPC;

                /* If we dont want to find players below a certain PCU limit filter here now */
                if (mustCheckPcu && config.MinPCUToBeFound > identity.BlockLimits.PCUBuilt)
                    continue;

                var modes = new List<PurchaseMode>();

                if (isNpc) {

                    bool validFaction = faction != null && faction.IsEveryoneNpc() && faction.Stations.Count > 0;

                    if (validFaction)
                        modes.Add(PurchaseMode.NPC);

                } else {

                    if (playerCollection.IsPlayerOnline(identityId)) {

                        modes.Add(PurchaseMode.ONLINE);

                    } else {

                        var lastSeen = PlayerUtils.GetLastSeenDate(identity);

                        /* How many minutes ago does still count as online? */
                        var maxOnlineDate = DateTime.Now.AddMinutes(-config.LastOnlineMinutes);

                        if (lastSeen >= maxOnlineDate)
                            modes.Add(PurchaseMode.ONLINE);

                        if (config.OfflineLongerThanHours > 0) {

                            /* How many hours ago does mean its inactive? */
                            var minOnlineInactiveDate = DateTime.Now.AddHours(-config.OfflineLongerThanHours);

                            if (lastSeen < minOnlineInactiveDate)
                                modes.Add(PurchaseMode.INACTIVE);
                        }
                    }
                }

                if (modes.Count > 0)
                    result.Add(identityId, modes);
            }

            return result;
        }

        private Dictionary<long, List<PurchaseMode>> FindFilteredBuyables(PurchaseMode mode) {

            var buyables = FindBuyables();

            if (mode == PurchaseMode.RANDOM)
                return buyables;

            var filtered = new Dictionary<long, List<PurchaseMode>>();

            foreach (var entry in buyables) {

                long identityId = entry.Key;
                List<PurchaseMode> modes = entry.Value;

                if (!modes.Contains(mode))
                    continue;

                filtered.Add(identityId, modes);
            }

            return filtered;
        }

        private Dictionary<long, List<PurchaseMode>> FindFilteredBuyablesForPlayer(PurchaseMode mode) {

            var buyables = FindFilteredBuyables(mode);

            var filtered = new Dictionary<long, List<PurchaseMode>>();

            var player = Context.Player;
            var identityId = player.IdentityId;

            HashSet<long> factionMembers = new HashSet<long>(); 

            if (Plugin.Config.FilterFactionMembers) {

                try {

                    var faction = FactionUtils.GetPlayerFaction(identityId);

                    factionMembers = new HashSet<long>(faction.Members.Keys);

                } catch (Exception e) {
                    Log.Error(e, "Faction could not be checked, it potentially has no founder!");
                }
            }

            factionMembers.Add(identityId);

            foreach (var entry in buyables) {

                long id = entry.Key;

                if (factionMembers.Contains(id))
                    continue;

                filtered.Add(entry.Key, entry.Value);
            }

            return filtered;
        }
    }
}
