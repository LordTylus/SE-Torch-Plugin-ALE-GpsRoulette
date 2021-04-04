using Torch;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public class GpsRouletteConfig : ViewModel {

        private long _priceCreditsRandom = 100_000_000L; //100 million credits
        private long _priceCreditsOnline = 100_000_000L; //100 million credits
        private long _priceCreditsInactive = 100_000_000L; //100 million credits
        private long _priceCreditsNPC = 100_000_000L; //100 million credits

        private int _cooldownMinutes = 30; //30 Minutes
        private int _cooldownMinutesFactionChange = 0; //Disabled by Default

        private int _lastOnlineMinutes = 0;  //ignore offline players unless they are inactive
        private int _offlineLongerThanHours = 24 * 7; //offline players offline for more than 7 days

        private bool _includePlayersWithoutFaction = false; //Ignore players without faction
        private bool _filterFactionMembers = true; //Do not buy GPS of faction Members

        private bool _notifySoldPlayer = true; //Notify the player someone got their gps
        private int _notifyDelaySeconds = 0; //Notify immediately

        private int _minPCUToBeFound = 10_000; //People below 10k PCU wont be purchasable
        private int _gpsOffsetFromPlayerKm = 0; //Spot on
        private int _gpsOffsetFromPlayerKmMax = 0; //Spot on
        private bool _minPCUAlsoForNPC = true; //NPCs have to repsect the MinPCU as well. 

        private int _minPCUToBuy = 0;
        private bool _mustBeInFactionToBuy = false;
        private int _minOnlineMinutesToBuy = 0;
        private int _minPlayerOnlineToBuy = 0;

        private bool _useDynamicPrices = false;
        private float _dynamicPriceMultiplier = 1.0F;

        public long PriceCreditsRandom { get => _priceCreditsRandom; set => SetValue(ref _priceCreditsRandom, value); }
        public long PriceCreditsOnline { get => _priceCreditsOnline; set => SetValue(ref _priceCreditsOnline, value); }
        public long PriceCreditsInactive { get => _priceCreditsInactive; set => SetValue(ref _priceCreditsInactive, value); }
        public long PriceCreditsNPC { get => _priceCreditsNPC; set => SetValue(ref _priceCreditsNPC, value); }

        public int CooldownMinutes { get => _cooldownMinutes; set => SetValue(ref _cooldownMinutes, value); }
        public int CooldownMinutesFactionChange { get => _cooldownMinutesFactionChange; set => SetValue(ref _cooldownMinutesFactionChange, value); }
        public int LastOnlineMinutes { get => _lastOnlineMinutes; set => SetValue(ref _lastOnlineMinutes, value); }
        public int OfflineLongerThanHours { get => _offlineLongerThanHours; set => SetValue(ref _offlineLongerThanHours, value); }
        public bool IncludePlayersWithoutFaction { get => _includePlayersWithoutFaction; set => SetValue(ref _includePlayersWithoutFaction, value); }
        public bool FilterFactionMembers { get => _filterFactionMembers; set => SetValue(ref _filterFactionMembers, value); }
        public bool NotifySoldPlayer { get => _notifySoldPlayer; set => SetValue(ref _notifySoldPlayer, value); }
        public int NotifyDelaySeconds { get => _notifyDelaySeconds; set => SetValue(ref _notifyDelaySeconds, value); }
        public int MinPCUToBeFound { get => _minPCUToBeFound; set => SetValue(ref _minPCUToBeFound, value); }
        public int GpsOffsetFromPlayerKm { get => _gpsOffsetFromPlayerKm; set => SetValue(ref _gpsOffsetFromPlayerKm, value); }
        public int GpsOffsetFromPlayerKmMax { get => _gpsOffsetFromPlayerKmMax; set => SetValue(ref _gpsOffsetFromPlayerKmMax, value); }
        public bool MinPCUAlsoForNPC { get => _minPCUAlsoForNPC; set => SetValue(ref _minPCUAlsoForNPC, value); }

        public int MinPCUToBuy { get => _minPCUToBuy; set => SetValue(ref _minPCUToBuy, value); }
        public bool MustBeInFactionToBuy { get => _mustBeInFactionToBuy; set => SetValue(ref _mustBeInFactionToBuy, value); }
        public int MinOnlineMinutesToBuy { get => _minOnlineMinutesToBuy; set => SetValue(ref _minOnlineMinutesToBuy, value); }
        public int MinPlayerOnlineToBuy { get => _minPlayerOnlineToBuy; set => SetValue(ref _minPlayerOnlineToBuy, value); }

        public bool UseDynamicPrices { get => _useDynamicPrices; set => SetValue(ref _useDynamicPrices, value); }
        public float DynamicPriceMultiplier { get => _dynamicPriceMultiplier; set => SetValue(ref _dynamicPriceMultiplier, value); }
    }
}
