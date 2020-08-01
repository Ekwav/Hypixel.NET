using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Hypixel.NET.PlayerApi.Player.SocialMedia
{
    public class SocialMedia
    {
        [JsonProperty("links")]
        public Links Links;
    }
}
