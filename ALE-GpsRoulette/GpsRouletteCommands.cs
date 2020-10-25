using ALE_Core.Cooldown;
using ALE_Core.GridExport;
using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Utils;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.GameSystems.BankingAndCurrency;

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
                sb.AppendLine("- Bought locations will be directly on target.");
            else
                sb.AppendLine("- Bought locations will be about "+offset.ToString("#,##0")+" km away from the target.");

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

            var price = Plugin.Config.PriceCreditsRandom;
            if (price >= 0)
                sb.AppendLine(prefix+"!gps buy random -- for " + price.ToString("#,##0") + " SC");

            price = Plugin.Config.PriceCreditsOnline;
            if (price >= 0)
                sb.AppendLine(prefix + "!gps buy online -- for " + price.ToString("#,##0") + " SC");

            price = Plugin.Config.PriceCreditsInactive;
            if (price >= 0)
                sb.AppendLine(prefix + "!gps buy inactive -- for " + price.ToString("#,##0") + " SC");

            price = Plugin.Config.PriceCreditsNPC;
            if (price >= 0)
                sb.AppendLine(prefix + "!gps buy npc -- for " + price.ToString("#,##0") + " SC");
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

        private bool BuyRandomFromDict(Dictionary<long, List<PurchaseMode>> buyables) {

            List<long> buyablesAsList = new List<long>(buyables.Keys);
            buyablesAsList.ShuffleList();

            long identityId = Context.Player.IdentityId;
            long foundIdentity = buyablesAsList.First();

            var config = Plugin.Config;

            MyGps position = FindPositionOfPlayer(foundIdentity);

            if (position == null)
                return false;

            SendGps(position, identityId);

            var location = position.Coords;
            var otherPlayer = PlayerUtils.GetIdentityById(foundIdentity);

            if(otherPlayer != null)
                Log.Info("Player " + Context.Player.DisplayName + " bought GPS Location X: " + location.X + " Y: " + location.Y + " Z: " + location.Z + " of Player " + otherPlayer.DisplayName);

            if (config.NotifySoldPlayer) {

                MyVisualScriptLogicProvider.ShowNotification(
                    "Watch out! Someone bought your current location. He will be here soon!",
                    5000, "Red", foundIdentity);
            }

            return true;
        }

        private MyGps FindPositionOfPlayer(long foundIdentity) {
            throw new NotImplementedException();
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

                /* If we dont want to find players below a certain PCU limit filter here now */
                if (config.MinPCUToBeFound > identity.BlockLimits.PCUBuilt)
                    continue;

                var modes = new List<PurchaseMode>();

                if (playerCollection.IdentityIsNpc(identityId)) {

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
