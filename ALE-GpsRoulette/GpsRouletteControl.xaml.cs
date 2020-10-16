using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
