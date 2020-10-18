using Torch;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public class GpsRouletteConfig : ViewModel {

        private long _priceCreditsRandom = 100_000_000L; //100 million credits
        private long _priceCreditsOnline = 100_000_000L; //100 million credits
        private long _priceCreditsInactive = 100_000_000L; //100 million credits
        private long _priceCreditsNPC = 100_000_000L; //100 million credits

        private int _cooldownMinutes = 30; //30 Minutes

        private int _lastOnlineMinutes = 0;  //ignore offline players unless they are inactive
        private int _offlineLongerThanHours = 24 * 7; //offline players offline for more than 7 days

        private bool _includePlayersWithoutFaction = false; //Ignore players without faction
        private bool _includeOfflinePlayers = false; //Ignore players that are offline
        private bool _filterFactionMembers = true; //Do not buy GPS of faction Members

        private bool _notifySoldPlayer = true; //Dont notify the player someone got their gps

        private int _minPCUToBeFound = 10_000; //People below 10k PCU wont be purchasable
        private int _gpsOffsetFromPlayerKm = 0; //Spot on

        public long PriceCreditsRandom { get => _priceCreditsRandom; set => SetValue(ref _priceCreditsRandom, value); }
        public long PriceCreditsOnline { get => _priceCreditsOnline; set => SetValue(ref _priceCreditsOnline, value); }
        public long PriceCreditsInactive { get => _priceCreditsInactive; set => SetValue(ref _priceCreditsInactive, value); }
        public long PriceCreditsNPC { get => _priceCreditsNPC; set => SetValue(ref _priceCreditsNPC, value); }

        public int CooldownMinutes { get => _cooldownMinutes; set => SetValue(ref _cooldownMinutes, value); }
        public int LastOnlineMinutes { get => _lastOnlineMinutes; set => SetValue(ref _lastOnlineMinutes, value); }
        public int OfflineLongerThanHours { get => _offlineLongerThanHours; set => SetValue(ref _offlineLongerThanHours, value); }
        public bool IncludePlayersWithoutFaction { get => _includePlayersWithoutFaction; set => SetValue(ref _includePlayersWithoutFaction, value); }
        public bool IncludeOfflinePlayers { get => _includeOfflinePlayers; set => SetValue(ref _includeOfflinePlayers, value); }
        public bool FilterFactionMembers { get => _filterFactionMembers; set => SetValue(ref _filterFactionMembers, value); }
        public bool NotifySoldPlayer { get => _notifySoldPlayer; set => SetValue(ref _notifySoldPlayer, value); }
        public int MinPCUToBeFound { get => _minPCUToBeFound; set => SetValue(ref _minPCUToBeFound, value); }
        public int GpsOffsetFromPlayerKm { get => _gpsOffsetFromPlayerKm; set => SetValue(ref _gpsOffsetFromPlayerKm, value); }

    }
}
