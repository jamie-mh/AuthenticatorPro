using System.Collections.Generic;
using System.Linq;

namespace ProAuth.Utilities
{
    internal static class Icons
    {
        public static readonly Dictionary<string, int> LightIcons = new Dictionary<string, int>
        {
            { "up", Resource.Drawable.ic_arrow_upward_light },
            { "folder", Resource.Drawable.ic_folder_light },
            { "file", Resource.Drawable.ic_insert_drive_file_light },
            { "proauth", Resource.Mipmap.ic_launcher },
            { "arrow_back", Resource.Drawable.ic_action_arrow_back_light }
        };

        public static readonly Dictionary<string, int> DarkIcons = new Dictionary<string, int>
        {
            { "up", Resource.Drawable.ic_arrow_upward_dark },
            { "folder", Resource.Drawable.ic_folder_dark },
            { "file", Resource.Drawable.ic_insert_drive_file_dark },
            { "proauth", Resource.Mipmap.ic_launcher },
            { "arrow_back", Resource.Drawable.ic_action_arrow_back_dark }
        };

        public static readonly Dictionary<string, int> Service = new Dictionary<string, int>
        {
            { "default", Resource.Drawable.auth_default },
            { "google", Resource.Drawable.auth_google },
            { "facebook", Resource.Drawable.auth_facebook },
            { "amazon", Resource.Drawable.auth_amazon },
            { "bitbucket", Resource.Drawable.auth_bitbucket },
            { "discord", Resource.Drawable.auth_discord },
            { "dropbox", Resource.Drawable.auth_dropbox },
            { "evernote", Resource.Drawable.auth_evernote },
            { "gandi", Resource.Drawable.auth_gandi },
            { "github", Resource.Drawable.auth_github },
            { "kickstarter", Resource.Drawable.auth_kickstarter },
            { "logmein", Resource.Drawable.auth_logmein },
            { "microsoft", Resource.Drawable.auth_microsoft },
            { "twitter", Resource.Drawable.auth_twitter },
            { "ovh", Resource.Drawable.auth_ovh },
            { "1and1", Resource.Drawable.auth_1and1 },
            { "adobe", Resource.Drawable.auth_adobe },
            { "algolia", Resource.Drawable.auth_algolia },
            { "aws", Resource.Drawable.auth_aws },
            { "azure", Resource.Drawable.auth_azure },
            { "battlenet", Resource.Drawable.auth_battlenet },
            { "digitalocean", Resource.Drawable.auth_digitalocean },
            { "epicgames", Resource.Drawable.auth_epicgames },
            { "gitlab", Resource.Drawable.auth_gitlab },
            { "gmail", Resource.Drawable.auth_gmail },
            { "godaddy", Resource.Drawable.auth_godaddy },
            { "googlecloudplatform", Resource.Drawable.auth_googlecloudplatform },
            { "googledrive", Resource.Drawable.auth_googledrive },
            { "googleplay", Resource.Drawable.auth_googleplay },
            { "guildwars2", Resource.Drawable.auth_guildwars2 },
            { "hangouts", Resource.Drawable.auth_hangouts },
            { "heroku", Resource.Drawable.auth_heroku },
            { "humblebundle", Resource.Drawable.auth_humblebundle },
            { "mailchimp", Resource.Drawable.auth_mailchimp },
            { "mega", Resource.Drawable.auth_mega },
            { "namecheap", Resource.Drawable.auth_namecheap },
            { "office365", Resource.Drawable.auth_office365 },
            { "onedrive", Resource.Drawable.auth_onedrive },
            { "origin", Resource.Drawable.auth_origin },
            { "outlook", Resource.Drawable.auth_outlook },
            { "protonmail", Resource.Drawable.auth_protonmail },
            { "reddit", Resource.Drawable.auth_reddit },
            { "rockstargames", Resource.Drawable.auth_rockstargames },
            { "slack", Resource.Drawable.auth_slack },
            { "twitch", Resource.Drawable.auth_twitch },
            { "uplay", Resource.Drawable.auth_uplay },
            { "xbox", Resource.Drawable.auth_xbox },
            { "youtube", Resource.Drawable.auth_youtube },
            { "binance", Resource.Drawable.auth_binance },
            { "bitcoin", Resource.Drawable.auth_bitcoin },
            { "bitfinex", Resource.Drawable.auth_bitfinex },
            { "blockchain", Resource.Drawable.auth_blockchain },
            { "coinbase", Resource.Drawable.auth_coinbase },
            { "cexio", Resource.Drawable.auth_cexio },
            { "kraken", Resource.Drawable.auth_kraken },
            { "ubisoft", Resource.Drawable.auth_ubisoft },
            { "electronicarts", Resource.Drawable.auth_electronicarts }
        };

        public static int GetIcon(string key)
        {
            return ThemeHelper.IsDark ? DarkIcons[key] : LightIcons[key];
        }

        public static int GetService(string key)
        {
            if(key == null || !Service.ContainsKey(key))
            {
                return Service["default"];
            }

            return Service[key];
        }

        public static string FindServiceKeyByName(string name)
        {
            string key  = name.ToLower().Split(' ')[0];
            return Service.Keys.Contains(key) ? key : "default";
        }
    }
}