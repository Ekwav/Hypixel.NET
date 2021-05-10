using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Timers;
using Hypixel.NET.BoosterApi;
using Hypixel.NET.FriendsApi;
using Hypixel.NET.GuildApi;
using Hypixel.NET.KeyApi;
using Hypixel.NET.LeaderboardsApi;
using Hypixel.NET.PlayerApi;
using Hypixel.NET.PlayerApi.Player.GameCounts;
using Hypixel.NET.SkyblockApi;
using Hypixel.NET.SkyblockApi.Bazaar;
using Hypixel.NET.SkyblockApi.News;
using Hypixel.NET.WatchdogStatsApi;
using Newtonsoft.Json;
using RestSharp;


namespace Hypixel.NET
{
    public class HypixelApi
    {
        private readonly string _apiKey;
        private readonly int _cacheStoreTime;
        private static int _apiRequests;
        private static readonly MemoryCache ApiMemoryCache = MemoryCache.Default;
        private Timer _apiResetTimer;

        public HypixelApi(string apiKey, int cacheTimeInSeconds)
        {
            _apiKey = apiKey;
            _cacheStoreTime = cacheTimeInSeconds;

            if (!IsApiKeyValid(apiKey))
            {
                var message = " Invalid API key. Please double check your information";
                var hypixelException = new ApplicationException(message);
                throw hypixelException;
            }

            _apiResetTimer = new Timer(60000);
            _apiResetTimer.AutoReset = false;
            _apiResetTimer.Elapsed += ResetApiLimit;
        }

        public AuctionsEnded getAuctionsEnded()
        {
            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock/");
            var request = new RestRequest($"auctions_ended?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<AuctionsEnded>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                //AddItemToCache(profileCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            var message = $"{responseDeserialized} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public bool IsApiKeyValid(string apiKey)
        {
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"key?key={apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetKey>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return true;
            }

            return false;
        }
        #region Caching
        private void AddItemToCache(string itemType, string apiResponse)
        {
            var cacheItemPolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(_cacheStoreTime),
            };

            ApiMemoryCache.Add(itemType, apiResponse, cacheItemPolicy);
        }
        #endregion

        #region Ratelimitng
        private void ResetApiLimit(object sender, ElapsedEventArgs e)
        {
            _apiRequests = 0;
        }

        private void RateLimitCheck()
        {
            if (!_apiResetTimer.Enabled)
            {
                _apiResetTimer = new Timer(60000);
                _apiResetTimer.Start();
                _apiResetTimer.AutoReset = false;
                _apiResetTimer.Elapsed += ResetApiLimit;
                return;
            }

            if (_apiRequests < 118)
            {
                return;
            }

            const string rateLimitHit = "Rate limit hit!";
            var hypixelRateLimitException = new ApplicationException(rateLimitHit);
            throw hypixelRateLimitException;
        }
        #endregion

        #region synchronous

        #region Player
        public GetPlayerData GetUserByUuid(string uuid)
        {
            ApplicationException hypixelException;

            //Multiple requests use uuids so append the type to the uuid
            var cacheUuid = uuid + "Type:PlayerData";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheUuid))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheUuid);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetPlayerData>(getCacheItem.Value.ToString().Replace(".0", ""));
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"player?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetPlayerData>(response.Content.Replace(".0", ""));

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful && responseDeserialized.Player != null)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheUuid, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //Hypixel API returns success at true though the player is null
            if (responseDeserialized.Player == null && responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                const string playerNotFound = "Invalid UUID! Please double check your UUID";
                hypixelException = new ApplicationException(playerNotFound, response.ErrorException);
                throw hypixelException;
            }

            var message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public GetPlayerData GetUserByPlayerName(string name)
        {
            ApplicationException hypixelException;

            //Check cache
            if (ApiMemoryCache.Contains(name))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(name);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetPlayerData>(getCacheItem.Value.ToString().Replace(".0", ""));
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Check rate limit
            RateLimitCheck();

            //Get the UUID of the player requested - support for name is no longer supported
            var uuid = GetUuidFromPlayerName(name);

            if (uuid == null)
            {
                hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"player?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetPlayerData>(response.Content.Replace(".0", ""));

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful && responseDeserialized.Player != null)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(name, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //Hypixel API returns success at true though the player is null
            if (responseDeserialized.Player == null && responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                const string playerNotFound = "Invalid player name! Please double check your player name";
                hypixelException = new ApplicationException(playerNotFound, response.ErrorException);
                throw hypixelException;
            }

            var message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Friends
        public GetFriends GetPlayerFriendsByUuid(string uuid)
        {
            var cacheUuid = uuid + "Type:Friends";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheUuid))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheUuid);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetFriends>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"friends?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetFriends>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheUuid, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public GetFriends GetPlayerFriendsByPlayerName(string playerName)
        {
            var playerUuid = GetUuidFromPlayerName(playerName);

            if (playerUuid == null)
            {
                var hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            return GetPlayerFriendsByUuid(playerUuid);
        }

        #endregion

        #region Guild
        public GetGuild GetGuildByGuildName(string guildName)
        {
            var cacheGuild = guildName + "Type:GuildName";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheGuild))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheGuild);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetGuild>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"guild?key={_apiKey}&name={guildName}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetGuild>(response.Content);

            string message;
            ApplicationException hypixelException;

            //Throw expection
            if (responseDeserialized.Guild == null)
            {
                _apiRequests = _apiRequests + 1;
                message = $"{responseDeserialized.Cause} That guild does not exist";
                hypixelException = new ApplicationException(message, response.ErrorException);
                throw hypixelException;
            }

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheGuild, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public GetGuild GetGuildByPlayerName(string playerName)
        {
            //convert the player name into a uuid - Hypixel doesn't allow guild requests by player names
            var playerUuid = GetUuidFromPlayerName(playerName);

            if (playerUuid == null)
            {
                var hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            //use the get guild by uuid 
            return GetGuildByUuid(playerUuid);
        }

        public GetGuild GetGuildByUuid(string uuid)
        {
            var cacheGuild = uuid + "Type:GuildPlayerUuid";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheGuild))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheGuild);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetGuild>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"guild?key={_apiKey}&player={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetGuild>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheGuild, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Boosters
        public GetBoosters GetBoosters()
        {
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"boosters?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetBoosters>(response.Content.Replace(".0", ""));

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Api Key info
        public GetKey GetApiKeyInformation(string apiKey)
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"key?key={apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetKey>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Watchdog
        public GetWatchdogStats GetWatchdogStats()
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"watchdogstats?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetWatchdogStats>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Leaderboards
        public GetLeaderboards GetLeaderboards()
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"leaderboards?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetLeaderboards>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Game Counts
        public GetGameCounts GetGameCounts()
        {
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"gamecounts?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetGameCounts>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Skyblock
        public GetSkyBlockProfile GetSkyblockProfileByProfileId(string skyblockProfileId)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileCache = skyblockProfileId + "Type:SkyblockProfile";

            if (ApiMemoryCache.Contains(profileCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<GetSkyBlockProfile>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"profile?key={_apiKey}&profile={skyblockProfileId}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetSkyBlockProfile>(response.Content);

            //Throw expection
            if (responseDeserialized.Profile == null)
            {
                _apiRequests = _apiRequests + 1;
                message = "That profile ID does not exist";
                hypixelException = new ApplicationException(message, response.ErrorException);
                throw hypixelException;
            }

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public List<GetSkyBlockProfile> GetSkyblockProfilesByName(string username)
        {
            //First grab the data from the username
            var userData = GetUserByPlayerName(username);

            //Make sure that the requests won't go over the amount
            if (_apiRequests + userData.Player.Stats.SkyBlock.Profiles.Count > 118)
            {
                var overRatelimitExceptiom = new ApplicationException("Rate limit would be hit getting all profiles!");
                throw overRatelimitExceptiom;
            }

            var profileList = new List<GetSkyBlockProfile>();

            //Go through each profile to get API response
            foreach (var profile in userData.Player.Stats.SkyBlock.Profiles)
            {
                profileList.Add(GetSkyblockProfileByProfileId(profile.Value.ProfileId));
            }

            return profileList;
        }

        public GetAuctionPage GetAuctionPage(int auctionPage)
        {
            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auctions?key={_apiKey}&page={auctionPage}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetAuctionPage>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public AuctionsByPlayer GetAuctionsByPlayerUuid(string uuid)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileCache = uuid + "Type:SkyblockAuctionsByUuid";

            if (ApiMemoryCache.Contains(profileCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<AuctionsByPlayer>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock/");
            var request = new RestRequest($"auction?key={_apiKey}&player={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<AuctionsByPlayer>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public AuctionsByPlayer GetAuctionsByPlayerName(string name)
        {
            ApplicationException hypixelException;

            //Get UUID from the player name
            var uuid = GetUuidFromPlayerName(name);

            if (uuid == null)
            {
                hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            return GetAuctionsByPlayerUuid(uuid);
        }

        public AuctionsByProfile GetAuctionsByProfileId(string id)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileIdCache = id + "Type:SkyblockAuctionsByProfileId";

            if (ApiMemoryCache.Contains(profileIdCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileIdCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<AuctionsByProfile>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auction?key={_apiKey}&profile={id}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<AuctionsByProfile>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileIdCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public AuctionByAuctionId GetAuctionByAuctionId(string id)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var auctionIdCache = id + "Type:SkyblockAuctionByAuctionId";

            if (ApiMemoryCache.Contains(auctionIdCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(auctionIdCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = JsonConvert.DeserializeObject<AuctionByAuctionId>(getCacheItem.Value.ToString());
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auction?key={_apiKey}&uuid={id}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<AuctionByAuctionId>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(auctionIdCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public GetNews GetNews()
        {
            string message;
            ApplicationException hypixelException;

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"news?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetNews>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public GetBazaarProducts GetBazaarProducts()
        {
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"skyblock/bazaar?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);
            var responseDeserialized = JsonConvert.DeserializeObject<GetBazaarProducts>(response.Content);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Mojang
        private string GetUuidFromPlayerName(string playerName)
        {
            //Create the request
            var client = new RestClient("https://api.mojang.com/");
            var request = new RestRequest($"users/profiles/minecraft/{playerName}", Method.GET);

            //Get the response and Deserialize
            var response = client.Execute(request);

            if (response.Content == "")
            {
                return null;
            }

            dynamic responseDeserialized = JsonConvert.DeserializeObject(response.Content);

            //Mojang stores the uuid under id so return that
            return responseDeserialized.id;
        }
        #endregion

        #endregion

        #region Async

        #region Player
        public async Task <GetPlayerData> GetUserByUuidAsync(string uuid)
        {
            ApplicationException hypixelException;

            //Multiple requests use uuids so append the type to the uuid
            var cacheUuid = uuid + "Type:PlayerData";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheUuid))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheUuid);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetPlayerData>(getCacheItem.Value.ToString().Replace(".0", ""))).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"player?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetPlayerData>(response.Content.Replace(".0", ""))).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful && responseDeserialized.Player != null)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheUuid, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //Hypixel API returns success at true though the player is null
            if (responseDeserialized.Player == null && responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                const string playerNotFound = "Invalid UUID! Please double check your UUID";
                hypixelException = new ApplicationException(playerNotFound, response.ErrorException);
                throw hypixelException;
            }

            var message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task<GetPlayerData> GetUserByPlayerNameAsync(string name)
        {
            ApplicationException hypixelException;

            //Check cache
            if (ApiMemoryCache.Contains(name))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(name);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetPlayerData>(getCacheItem.Value.ToString().Replace(".0", ""))).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Check rate limit
            RateLimitCheck();

            //Get the UUID of the player requested - support for name is no longer supported
            var uuid = await GetUuidFromPlayerNameAsync(name).ConfigureAwait(false);

            if (uuid == null)
            {
                hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"player?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetPlayerData>(response.Content.Replace(".0", ""))).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful && responseDeserialized.Player != null)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(name, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //Hypixel API returns success at true though the player is null
            if (responseDeserialized.Player == null && responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                const string playerNotFound = "Invalid player name! Please double check your player name";
                hypixelException = new ApplicationException(playerNotFound, response.ErrorException);
                throw hypixelException;
            }

            var message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Friends
        public async Task<GetFriends> GetPlayerFriendsByUuidAsync(string uuid)
        {
            var cacheUuid = uuid + "Type:Friends";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheUuid))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheUuid);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetFriends>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"friends?key={_apiKey}&uuid={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetFriends>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheUuid, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task <GetFriends> GetPlayerFriendsByPlayerNameAsync(string playerName)
        {
            var playerUuid = await GetUuidFromPlayerNameAsync(playerName).ConfigureAwait(false);

            if (playerUuid == null)
            {
                var hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            return await GetPlayerFriendsByUuidAsync(playerUuid).ConfigureAwait(false);
        }

        #endregion

        #region Guild
        public async Task <GetGuild> GetGuildByGuildNameAsync(string guildName)
        {
            var cacheGuild = guildName + "Type:GuildName";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheGuild))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheGuild);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetGuild>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"guild?key={_apiKey}&name={guildName}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetGuild>(response.Content)).ConfigureAwait(false);

            string message;
            ApplicationException hypixelException;

            //Throw expection
            if (responseDeserialized.Guild == null)
            {
                _apiRequests = _apiRequests + 1;
                message = $"{responseDeserialized.Cause} That guild does not exist";
                hypixelException = new ApplicationException(message, response.ErrorException);
                throw hypixelException;
            }

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheGuild, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized.Cause} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task<GetGuild> GetGuildByPlayerNameAsync(string playerName)
        {
            //convert the player name into a uuid - Hypixel doesn't allow guild requests by player names
            var playerUuid = await GetUuidFromPlayerNameAsync(playerName).ConfigureAwait(false);

            if (playerUuid == null)
            {
                var hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            //use the get guild by uuid 

            return await GetGuildByUuidAsync(playerUuid).ConfigureAwait(false);
        }

        public async Task <GetGuild> GetGuildByUuidAsync(string uuid)
        {
            var cacheGuild = uuid + "Type:GuildPlayerUuid";

            //Check if cached. If so deserialize and return
            if (ApiMemoryCache.Contains(cacheGuild))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(cacheGuild);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetGuild>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            //Rate limit check
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"guild?key={_apiKey}&player={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetGuild>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(cacheGuild, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Boosters
        public async Task <GetBoosters> GetBoostersAsync()
        {
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"boosters?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetBoosters>(response.Content.Replace(".0", ""))).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Api Key info
        public async Task <GetKey> GetApiKeyInformationAsync(string apiKey)
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"key?key={apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetKey>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Watchdog
        public async Task <GetWatchdogStats> GetWatchdogStatsAsync()
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"watchdogstats?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetWatchdogStats>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Leaderboards
        public async Task <GetLeaderboards> GetLeaderboardsAsync()
        {
            RateLimitCheck();
            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"leaderboards?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run (() => JsonConvert.DeserializeObject<GetLeaderboards>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Game Counts
        public async Task <GetGameCounts> GetGameCountsAsync()
        {
            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"gamecounts?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetGameCounts>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Skyblock
        public async Task <GetSkyBlockProfile> GetSkyblockProfileByProfileIdAsync(string skyblockProfileId)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileCache = skyblockProfileId + "Type:SkyblockProfile";

            if (ApiMemoryCache.Contains(profileCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<GetSkyBlockProfile>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"profile?key={_apiKey}&profile={skyblockProfileId}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetSkyBlockProfile>(response.Content)).ConfigureAwait(false);

            //Throw expection
            if (responseDeserialized.Profile == null)
            {
                _apiRequests = _apiRequests + 1;
                message = "That profile ID does not exist";
                hypixelException = new ApplicationException(message, response.ErrorException);
                throw hypixelException;
            }

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task<List<GetSkyBlockProfile>> GetSkyblockProfilesByNameAsync(string username)
        {
            //First grab the data from the username
            var userData = await GetUserByPlayerNameAsync(username).ConfigureAwait(false);

            //Make sure that the requests won't go over the amount
            if (_apiRequests + userData.Player.Stats.SkyBlock.Profiles.Count > 120)
            {
                var overRatelimitExceptiom = new ApplicationException("Rate limit would be hit getting all profiles!");
                throw overRatelimitExceptiom;
            }

            var profileList = new List<GetSkyBlockProfile>();

            //Go through each profile to get API response
            foreach (var profile in userData.Player.Stats.SkyBlock.Profiles)
            {
                profileList.Add(GetSkyblockProfileByProfileId(profile.Value.ProfileId));
            }

            return profileList;
        }

        public async Task <GetAuctionPage> GetAuctionPageAsync(int auctionPage)
        {
            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auctions?key={_apiKey}&page={auctionPage}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetAuctionPage>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task <AuctionsByPlayer> GetAuctionsByPlayerUuidAsync(string uuid)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileCache = uuid + "Type:SkyblockAuctionsByUuid";

            if (ApiMemoryCache.Contains(profileCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<AuctionsByPlayer>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock/");
            var request = new RestRequest($"auction?key={_apiKey}&player={uuid}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<AuctionsByPlayer>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task<AuctionsByPlayer> GetAuctionsByPlayerNameAsync(string name)
        {
            ApplicationException hypixelException;

            //Get UUID from the player name
            var uuid = await GetUuidFromPlayerNameAsync(name).ConfigureAwait(false);

            if (uuid == null)
            {
                hypixelException = new ApplicationException("Username not found!");
                throw hypixelException;
            }

            return await GetAuctionsByPlayerUuidAsync(uuid).ConfigureAwait(false);
        }

        public async Task <AuctionsByProfile> GetAuctionsByProfileIdAsync(string id)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var profileIdCache = id + "Type:SkyblockAuctionsByProfileId";

            if (ApiMemoryCache.Contains(profileIdCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(profileIdCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<AuctionsByProfile>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auction?key={_apiKey}&profile={id}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<AuctionsByProfile>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(profileIdCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task <AuctionByAuctionId> GetAuctionByAuctionIdAsync(string id)
        {
            string message;
            ApplicationException hypixelException;

            //Check if cached. If so deserialize and return
            var auctionIdCache = id + "Type:SkyblockAuctionByAuctionId";

            if (ApiMemoryCache.Contains(auctionIdCache))
            {
                var getCacheItem = ApiMemoryCache.GetCacheItem(auctionIdCache);

                //Verify that this isn't null - if is then will do API request as normal
                if (getCacheItem != null)
                {
                    var deserializedResponseCache = await Task.Run(() => JsonConvert.DeserializeObject<AuctionByAuctionId>(getCacheItem.Value.ToString())).ConfigureAwait(false);
                    deserializedResponseCache.FromCache = true;
                    return deserializedResponseCache;
                }
            }

            RateLimitCheck();

            //Create the request
            var client = new RestClient("https://api.hypixel.net/skyblock");
            var request = new RestRequest($"auction?key={_apiKey}&uuid={id}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<AuctionByAuctionId>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                AddItemToCache(auctionIdCache, response.Content);
                responseDeserialized.FromCache = false;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            message = $"{responseDeserialized} Please double check your request information";
            hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }

        public async Task<GetBazaarProducts> GetBazaarProductsAsync()
        {

            //Create the request
            var client = new RestClient("https://api.hypixel.net/");
            var request = new RestRequest($"skyblock/bazaar?key={_apiKey}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            var responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject<GetBazaarProducts>(response.Content)).ConfigureAwait(false);

            //Verify that the request was successful
            if (responseDeserialized.WasSuccessful)
            {
                _apiRequests = _apiRequests + 1;
                return responseDeserialized;
            }

            //If the response wasn't successful, an exception will be thrown
            var message = $"{responseDeserialized.Cause} Please double check your request information";
            var hypixelException = new ApplicationException(message, response.ErrorException);
            throw hypixelException;
        }
        #endregion

        #region Mojang
        private async Task <string> GetUuidFromPlayerNameAsync(string playerName)
        {
            //Create the request
            var client = new RestClient("https://api.mojang.com/");
            var request = new RestRequest($"users/profiles/minecraft/{playerName}", Method.GET);

            //Get the response and Deserialize
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);

            if (response.Content == "")
            {
                return null;
            }

            dynamic responseDeserialized = await Task.Run(() => JsonConvert.DeserializeObject(response.Content)).ConfigureAwait(false);

            //Mojang stores the uuid under id so return that
            return responseDeserialized.id;
        }
        #endregion

        #endregion
    }
}
