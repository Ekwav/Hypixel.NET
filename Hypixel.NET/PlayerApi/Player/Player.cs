using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hypixel.NET.PlayerApi.Player
{
    public class Player
    {
        /// <summary>
        /// General onetime unlock achievements
        /// </summary>
        [JsonProperty("achievementsOneTime")]
        public List<object> AchievementsOneTime { get; private set; }

        /// <summary>
        /// The display name of the player
        /// </summary>
        [JsonProperty("displayname")]
        public string DisplayName { get; private set; }

        /// <summary>
        /// The First time the player logged into Hypixel
        /// </summary>
        [JsonProperty("firstLogin")]
        private readonly long _firstLogin;
        public DateTime FirstLogin
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_firstLogin).ToLocalTime();
                return convertToDateTime;
            }
        }

        /// <summary>
        /// The last time the player logged into Hypixel
        /// </summary>
        [JsonProperty("lastLogin")]
        private readonly long _lastLogin;
        public DateTime LastLogin
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_lastLogin).ToLocalTime();
                return convertToDateTime;
            }
        }
        /// <summary>
        /// Known previous usernames that the player has had
        /// </summary>
        [JsonProperty("knownAliasesLower")]
        public List<string> KnownAliases { get; private set; }

        /// <summary>
        /// The players total xp on the server
        /// </summary>
        [JsonProperty("networkExp")]
        public long NetworkExp { get; private set; }

        /// <summary>
        /// The settings that the requested user has set
        /// </summary>
        [JsonProperty("settings")]
        public Settings.Settings Settings { get; private set; }

        /// <summary>
        /// The stats of each game that the user has played
        /// </summary>
        [JsonProperty("stats")]
        public Stats.Stats Stats { get; private set; }

        [JsonProperty("timePlaying")]
        public long TimePlaying { get; private set; }

        /// <summary>
        /// The UUID of the player
        /// </summary>
        [JsonProperty("uuid")]
        public string Uuid { get; private set; }

        [JsonProperty("karma")]
        public long Karma { get; private set; }

        [JsonProperty("vanityTokens")]
        public long VanityTokens { get; private set; }

        /// <summary>
        /// cosmetic packages
        /// </summary>
        [JsonProperty("vanityMeta")]
        public VanityMeta.VanityMeta VanityMeta { get; private set; }

        [JsonProperty("totalRewards")]
        public int TotalRewards { get; private set; }

        [JsonProperty("totalDailyRewards")]
        public int TotalDailyRewards { get; private set; }

        [JsonProperty("rewardStreak")]
        public int RewardStreak { get; private set; }

        [JsonProperty("rewardScore")]
        public int RewardScore { get; private set; }

        [JsonProperty("rewardHighScore")]
        public int RewardHighScore { get; private set; }

        [JsonProperty("currentPet")]
        public string CurrentPet { get; private set; }

        [JsonProperty("giftingMeta")]
        public GiftingMeta.GiftingMeta GiftingMeta { get; private set; }

        [JsonProperty("lastLogout")]
        private readonly long _lastLogout;
        public DateTime LastLogout
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_lastLogout).ToLocalTime();
                return convertToDateTime;
            }
        }

        [JsonProperty("rankPlusColor")]
        public string RankPlusColor { get; private set; }

        [JsonProperty("voting")]
        public Voting.Voting Voting;

        [JsonProperty("newPackageRank")]
        public string NewPackageRank { get; private set; }

        [JsonProperty("monthlyPackageRank")]
        public string MonthlyPackageRank { get; private set; }

        [JsonProperty("mostRecentMonthlyPackageRank")]
        public string MostRecentMonthlyPackageRank { get; private set; }

        [JsonProperty("socialMedia")]
        public SocialMedia.SocialMedia SocialMedia;


    }
}
