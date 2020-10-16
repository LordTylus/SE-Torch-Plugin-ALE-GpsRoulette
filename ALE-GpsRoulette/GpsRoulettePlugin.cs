using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using Torch.Session;
using VRage.Game;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public class GpsRoulettePlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private GpsRouletteControl _control;
        public UserControl GetControl() => _control ?? (_control = new GpsRouletteControl(this));

        private Persistent<GpsRouletteConfig> _config;
        public GpsRouletteConfig Config => _config?.Data;

        public CooldownManager CooldownManager { get; } = new CooldownManager();
        public CooldownManager ConfirmationManager { get; } = new CooldownManager();

        public override void Init(ITorchBase torch) {
            
            base.Init(torch);

            SetupConfig();
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
    }
}
