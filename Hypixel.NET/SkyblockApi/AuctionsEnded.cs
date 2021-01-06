using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Hypixel.NET.SkyblockApi.Auctions;

namespace Hypixel.NET.SkyblockApi
{
    public class AuctionsEnded
    {
        public bool FromCache;

        [JsonProperty("success")]
        public bool WasSuccessful { get; private set; }

        [JsonProperty("auctions")]
        public List<BinAuctionEnded> Auctions { get; private set; }
    }

    public class BinAuctionEnded
    {
        [JsonProperty("auction_id")]
        public string Uuid { get; private set; }

        [JsonProperty("seller")]
        public string Seller { get; private set; }

        [JsonProperty("seller_profile")]
        public string ProfileId { get; private set; }

        [JsonProperty("buyer")]
        public string Buyer { get; private set; }


        [JsonProperty("timestamp")]
        private readonly long _timestamp;
        public DateTime TimeStamp
        {
            get
            {
                var convertToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                convertToDateTime = convertToDateTime.AddMilliseconds(_timestamp).ToLocalTime();
                return convertToDateTime;
            }
        }


        [JsonProperty("price")]
        public int Price { get; private set; }

        [JsonProperty("bin")]
        public bool BuyItemNow { get; private set; }

        [JsonProperty("item_bytes")]
        public string ItemBytes { get; private set; }
    }
}
