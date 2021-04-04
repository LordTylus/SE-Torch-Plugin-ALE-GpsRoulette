using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VRage.Game;
using VRage.Game.ModAPI;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public class GpsRoulettePlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static readonly string COOLDOWN_COMMAND = "buy";
        public static readonly string NOTIFICATION_COOLDOWN_COMMAND = "notify";

        private static readonly int SKIPPED_UPDATES = 60;
        private int updateCount = 0;

        private GpsRouletteControl _control;
        public UserControl GetControl() => _control ?? (_control = new GpsRouletteControl(this));

        private Persistent<GpsRouletteConfig> _config;
        public GpsRouletteConfig Config => _config?.Data;

        public CooldownManager CooldownManager { get; } = new CooldownManager();
        public CooldownManager CooldownManagerFactionChange { get; } = new CooldownManager();
        public CooldownManager ConfirmationManager { get; } = new CooldownManager();

        private HashSet<EntityIdCooldownKey> NotificationQueue { get; } = new HashSet<EntityIdCooldownKey>();
        private CooldownManager CooldownManagerNotificationQueue { get; } = new CooldownManager();

        public override void Init(ITorchBase torch) {
            
            base.Init(torch);

            SetupConfig();

            TorchSessionManager torchSessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (torchSessionManager != null)
                torchSessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state) {

            switch (state) {

                case TorchSessionState.Loaded:

                    MySession.Static.Factions.OnPlayerJoined += PlayerJoinedFaction;
                    MySession.Static.Factions.OnPlayerLeft += PlayerLeftFaction;
                    MySession.Static.Factions.FactionCreated += FactionCreated;
                    MySession.Static.Factions.FactionStateChanged += FactionStateChanged;

                    break;

                case TorchSessionState.Unloading:

                    MySession.Static.Factions.OnPlayerJoined -= PlayerJoinedFaction;
                    MySession.Static.Factions.OnPlayerLeft -= PlayerLeftFaction;
                    MySession.Static.Factions.FactionCreated -= FactionCreated;
                    MySession.Static.Factions.FactionStateChanged -= FactionStateChanged;

                    break;
            }
        }

        private void FactionStateChanged(MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId) {

            if (action != MyFactionStateChange.RemoveFaction)
                return;

            ChangeCooldownOnFactionChange(playerId);
        }

        private void PlayerJoinedFaction(MyFaction faction, long identityId) {
            ChangeCooldownOnFactionChange(identityId);
        }

        private void PlayerLeftFaction(MyFaction faction, long identityId) {
            ChangeCooldownOnFactionChange(identityId);
        }

        private void FactionCreated(long factionId) {

            var faction = FactionUtils.GetIdentityById(factionId);

            ChangeCooldownOnFactionChange(faction.FounderId);
        }

        private void ChangeCooldownOnFactionChange(long identityId) {

            var cooldownMs = Config.CooldownMinutesFactionChange * 60 * 1000L;
            
            ulong steamId = MySession.Static.Players.TryGetSteamId(identityId);
            var steamIdKey = new SteamIdCooldownKey(steamId);

            if (cooldownMs <= 0)
                CooldownManagerFactionChange.StopCooldown(steamIdKey);
            else
                CooldownManagerFactionChange.StartCooldown(steamIdKey, COOLDOWN_COMMAND, cooldownMs);

            Log.Info("Player " + PlayerUtils.GetPlayerNameById(identityId) + " was put into Cooldown for Faction Change");
        }

        private void SetupConfig() {

            var configFile = Path.Combine(StoragePath, "GpsRoulette.cfg");

            try {

                _config = Persistent<GpsRouletteConfig>.Load(configFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<GpsRouletteConfig>(configFile, new GpsRouletteConfig());
                Save();
            }
        }

        public void Save() {
            try {
                _config.Save();
                Log.Info("Configuration Saved.");
            } catch (IOException e) {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        public override void Update() {

            base.Update();

            /* If queue is empty take easy way out */
            if (NotificationQueue.Count == 0)
                return;

            /* Only Check Every X Updates and not every. */
            updateCount++;

            if (updateCount < SKIPPED_UPDATES)
                return;

            updateCount = 0;

            try {

                foreach(EntityIdCooldownKey cooldownKey in NotificationQueue.ToList()) {

                    if (CooldownManagerNotificationQueue.CheckCooldown(cooldownKey, NOTIFICATION_COOLDOWN_COMMAND, out _)) {

                        long identityId = cooldownKey.EntityId;

                        NotifyPlayer(identityId);

                        NotificationQueue.Remove(cooldownKey);
                    }
                }

            } catch(Exception e) {
                Log.Error(e, "Error on Update!");
            }
        }

        public void AddIdentityToNotifiticationQueue(long identityId) {

            var cooldownKey = new EntityIdCooldownKey(identityId);
            var delay = Config.NotifyDelaySeconds * 1000L;

            CooldownManagerNotificationQueue.StartCooldown(cooldownKey, NOTIFICATION_COOLDOWN_COMMAND, delay);

            NotificationQueue.Add(cooldownKey);
        }

        public void NotifyPlayer(long identityId) {

            string message;

            if (Config.NotifyDelaySeconds <= 0) 
                message = "Watch out! Someone bought your current location. They will be here soon!";
            else 
                message = "Watch out! Someone bought your current location within the last " + Config.NotifyDelaySeconds.ToString("#,##0") + " seconds. They will be here soon!";

            MyVisualScriptLogicProvider.ShowNotification(
                message, 10000, MyFontEnum.White, identityId);

            MyVisualScriptLogicProvider.SendChatMessage(
                message, Torch.Config.ChatName, identityId, MyFontEnum.Red);

            Log.Info("Identity " + identityId + " (" + PlayerUtils.GetPlayerNameById(identityId) + ") was Notified about their GPS being bought!");
        }
    }
}
