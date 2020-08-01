using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hypixel.NET.SkyblockApi.Auctions
{
    public class Auction
    {
        [JsonProperty("_id")]
        public string Id { get; private set; }

        [JsonProperty("uuid")]
        public string Uuid { get; private set; }

        [JsonProperty("auctioneer")]
        public string Auctioneer { get; private set; }

        [JsonProperty("profile_id")]
        public string ProfileId { get; private set; }

        [JsonProperty("coop")]
        public List<string> Coop { get; private set; }

        [JsonProperty("start")]
        private readonly long _start;
        public DateTime Start
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_start).ToLocalTime();
                return convertToDateTime;
            }
        }

        [JsonProperty("end")]
        private readonly long _end;
        public DateTime End
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_end).ToLocalTime();
                return convertToDateTime;
            }
        }

        [JsonProperty("item_name")]
        public string ItemName { get; private set; }

        [JsonProperty("item_lore")]
        public string ItemLore { get; private set; }

        [JsonProperty("extra")]
        public string Extra { get; private set; }

        [JsonProperty("category")]
        public string Category { get; private set; }

        [JsonProperty("tier")]
        public string Tier { get; private set; }

        [JsonProperty("starting_bid")]
        public long StartingBid { get; private set; }

        [JsonProperty("item_bytes")]
        public ItemBytes ItemBytes { get; private set; }

        [JsonProperty("claimed")]
        public bool Claimed { get; private set; }

        [JsonProperty("bin")]
        public bool BuyItNow { get; private set; }

        [JsonProperty("claimed_bidders")]
        public List<string> ClaimedBidders { get; private set; }

        [JsonProperty("highest_bid_amount")]
        public long HighestBidAmount { get; private set; }

        [JsonProperty("bids")]
        public List<Bid> Bids { get; private set; }
    }
}
