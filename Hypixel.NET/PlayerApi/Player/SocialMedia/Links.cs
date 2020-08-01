using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Hypixel.NET.PlayerApi.Player.SocialMedia
{
    public class Links
    {
        [JsonProperty("HYPIXEL")]
        public string Hypixel { get; private set; }

        [JsonProperty("TWITTER")]
        public string Twitter { get; private set; }

        [JsonProperty("INSTAGRAM")]
        public string Instagram { get; private set; }

        [JsonProperty("DISCORD")]
        public string Discord { get; private set; }

        [JsonProperty("YOUTUBE")]
        public string Youtube { get; private set; }

        [JsonProperty("TWITCH")]
        public string Twitch { get; private set; }

        [JsonProperty("MIXER")]
        [ObsoleteAttribute("The Mixer service has closed. Wait until the API is updated to support Facebook Gaming or is removed altogether.", true)]
        public string Mixer { get; private set; }
    }
}
