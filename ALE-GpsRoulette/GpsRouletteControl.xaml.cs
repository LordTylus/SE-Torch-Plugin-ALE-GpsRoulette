using System.Windows;
using System.Windows.Controls;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public partial class GpsRouletteControl : UserControl {
        
        private GpsRoulettePlugin Plugin { get; }

        private GpsRouletteControl() {
            InitializeComponent();
        }

        public GpsRouletteControl(GpsRoulettePlugin plugin) : this() {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e) {
            Plugin.Save();
        }
    }
}
