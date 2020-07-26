using System;
using System.Threading.Tasks;
using Hypixel.NET;

namespace Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hypixel = new HypixelApi("YOUR API KEY", 300);

            #region Synchronous
            //Synchronous requests
            var playerRequest = hypixel.GetUserByPlayerName("barrows");
            Console.WriteLine(playerRequest.Player.Stats.SkyWars.Kills);

            var playerUuidRequest = hypixel.GetUserByUuid("4c38f0a6-a36f-4f06-985c-7851b3853ccb");
            Console.WriteLine(playerUuidRequest.Player.Stats.SkyWars.ArrowsShot);

            var guildByGuildName = hypixel.GetGuildByGuildName("Develop");
            Console.WriteLine(guildByGuildName.Guild.Coins);

            var guildByPlayerName = hypixel.GetGuildByPlayerName("barrows");
            Console.WriteLine(guildByPlayerName.Guild.Exp);

            var guildByUuid = hypixel.GetGuildByUuid("4c38f0a6-a36f-4f06-985c-7851b3853ccb");
            Console.WriteLine(guildByUuid.Guild.Name);

            var getFriendsByUuid = hypixel.GetPlayerFriendsByUuid("4c38f0a6-a36f-4f06-985c-7851b3853ccb");
            Console.WriteLine(getFriendsByUuid.Records.Count);

            var getFriendsByPlayerName = hypixel.GetPlayerFriendsByPlayerName("barrows");
            Console.WriteLine(getFriendsByPlayerName.Records.Count);

            var getBoosters = hypixel.GetBoosters();
            Console.WriteLine(getBoosters.Boosters.Count);

            var watchdogStats = hypixel.GetWatchdogStats();
            Console.WriteLine(watchdogStats.WatchdogTotal);

            var getApiKey = hypixel.GetApiKeyInformation("YOUR API KEY");
            Console.WriteLine(getApiKey.Record.TotalQueries);

            var getLeaderboards = hypixel.GetLeaderboards();
            Console.WriteLine(getLeaderboards.Leaderboards.SkyWars.Count);

            var getGameCounts = hypixel.GetGameCounts();
            Console.WriteLine(getGameCounts.Games.Limbo.Players);

            var getSkyblockProfile = hypixel.GetSkyblockProfileByProfileId("4c38f0a6a36f4f06985c7851b3853ccb");
            foreach (var member in getSkyblockProfile.Profile.Members)
            {
                Console.WriteLine(member.Value.PlayerStats.AuctionsBids);
            }

            var getProfilesByName = hypixel.GetSkyblockProfilesByName("barrows");
            foreach (var profile in getProfilesByName)
            {
                Console.WriteLine(profile.Profile.ProfileId);
            }

            var getAuctionPage = hypixel.GetAuctionPage(0);
            Console.WriteLine(getAuctionPage.Auctions[0].End);

            var auctionsByPlayerUuid = hypixel.GetAuctionsByPlayerUuid("4c38f0a6a36f4f06985c7851b3853ccb");
            Console.WriteLine(auctionsByPlayerUuid.Auctions[0].Auctioneer);

            var auctionsByPlayerName = hypixel.GetAuctionsByPlayerName("barrows");
            Console.WriteLine(auctionsByPlayerName.Auctions[0].Start);

            var auctionsBySkyblockProfile = hypixel.GetAuctionsByProfileId("4c38f0a6a36f4f06985c7851b3853ccb");
            Console.WriteLine(auctionsBySkyblockProfile.Auctions[0].ItemName);

            var auctionsBySkyblockAuctionId = hypixel.GetAuctionByAuctionId("6a576eeb8f6e4941a72844147c378b65");
            Console.WriteLine(auctionsBySkyblockAuctionId.Auction[0].ItemName);

            var getNews = hypixel.GetNews();
            Console.WriteLine(getNews.Items[0].Title);

            var bazaarProducts = hypixel.GetBazaarProducts();
            Console.WriteLine(bazaarProducts.Products["ENCHANTED_COAL"].QuickStatus.BuyPrice);

            var socialMediaRequest = hypixel.GetUserByPlayerName("Artu");
            Console.WriteLine(socialMediaRequest.Player.SocialMedia.Links.Instagram);
            #endregion

            #region Async
            //Async requests
            var playerRequestAsync = await hypixel.GetUserByPlayerNameAsync("barrows").ConfigureAwait(false);
            Console.WriteLine(playerRequestAsync.Player.Stats.SkyWars.Kills);

            var playerUuidRequestAsync = await hypixel.GetUserByUuidAsync("4c38f0a6-a36f-4f06-985c-7851b3853ccb").ConfigureAwait(false);
            Console.WriteLine(playerUuidRequestAsync.Player.Stats.SkyWars.ArrowsShot);

            var guildByGuildNameAsync = await hypixel.GetGuildByGuildNameAsync("Develop").ConfigureAwait(false);
            Console.WriteLine(guildByGuildNameAsync.Guild.Coins);

            var guildByPlayerNameAsync = await hypixel.GetGuildByPlayerNameAsync("barrows").ConfigureAwait(false);
            Console.WriteLine(guildByPlayerNameAsync.Guild.Exp);

            var guildByUuidAsync = await hypixel.GetGuildByUuidAsync("4c38f0a6-a36f-4f06-985c-7851b3853ccb").ConfigureAwait(false);
            Console.WriteLine(guildByUuidAsync.Guild.Name);

            var getFriendsByUuidAsync = await hypixel.GetPlayerFriendsByUuidAsync("4c38f0a6-a36f-4f06-985c-7851b3853ccb").ConfigureAwait(false);
            Console.WriteLine(getFriendsByUuidAsync.Records.Count);

            var getFriendsByPlayerNameAsync = await hypixel.GetPlayerFriendsByPlayerNameAsync("barrows").ConfigureAwait(false);
            Console.WriteLine(getFriendsByPlayerNameAsync.Records.Count);

            var getBoostersAsync = await hypixel.GetBoostersAsync().ConfigureAwait(false);
            Console.WriteLine(getBoostersAsync.Boosters.Count);

            var watchdogStatsAsync = await hypixel.GetWatchdogStatsAsync().ConfigureAwait(false);
            Console.WriteLine(watchdogStatsAsync.WatchdogTotal);

            var getApiKeyAsync = await hypixel.GetApiKeyInformationAsync("YOUR API KEY").ConfigureAwait(false);
            Console.WriteLine(getApiKeyAsync.Record.TotalQueries);

            var getLeaderboardsAsync = await hypixel.GetLeaderboardsAsync().ConfigureAwait(false);
            Console.WriteLine(getLeaderboardsAsync.Leaderboards.SkyWars.Count);

            var getGameCountsAsync = await hypixel.GetGameCountsAsync().ConfigureAwait(false);
            Console.WriteLine(getGameCountsAsync.Games.Limbo.Players);

            var getSkyblockProfileAsync = await hypixel.GetSkyblockProfileByProfileIdAsync("4c38f0a6a36f4f06985c7851b3853ccb").ConfigureAwait(false);
            foreach (var member in getSkyblockProfileAsync.Profile.Members)
            {
                Console.WriteLine(member.Value.PlayerStats.AuctionsBids);
            }

            var getProfilesByNameAsync = await hypixel.GetSkyblockProfilesByNameAsync("barrows").ConfigureAwait(false);
            foreach (var profile in getProfilesByNameAsync)
            {
                Console.WriteLine(profile.Profile.ProfileId);
            }

            var getAuctionPageAsync = await hypixel.GetAuctionPageAsync(0).ConfigureAwait(false);
            Console.WriteLine(getAuctionPageAsync.Auctions[0].End);

            var auctionsByPlayerUuidAsync = await hypixel.GetAuctionsByPlayerUuidAsync("4c38f0a6a36f4f06985c7851b3853ccb").ConfigureAwait(false);
            Console.WriteLine(auctionsByPlayerUuidAsync.Auctions[0].Auctioneer);

            var auctionsByPlayerNameAsync = await hypixel.GetAuctionsByPlayerNameAsync("barrows").ConfigureAwait(false);
            Console.WriteLine(auctionsByPlayerNameAsync.Auctions[0].Start);

            var auctionsBySkyblockProfileAsync = await hypixel.GetAuctionsByProfileIdAsync("4c38f0a6a36f4f06985c7851b3853ccb").ConfigureAwait(false);
            Console.WriteLine(auctionsBySkyblockProfileAsync.Auctions[0].ItemName);

            var auctionsBySkyblockAuctionIdAsync = await hypixel.GetAuctionByAuctionIdAsync("6a576eeb8f6e4941a72844147c378b65").ConfigureAwait(false);
            Console.WriteLine(auctionsBySkyblockAuctionIdAsync.Auction[0].ItemName);

            var bazaarProductsAsync = await hypixel.GetBazaarProductsAsync();
            Console.WriteLine(bazaarProducts.Products["ENCHANTED_COAL"].QuickStatus.BuyPrice);

            var socialMediaRequestAsync = await hypixel.GetUserByPlayerNameAsync("Artu").ConfigureAwait(false);
            Console.WriteLine(socialMediaRequestAsync.Player.SocialMedia.Links.Discord);
            #endregion
        }
    }
}
