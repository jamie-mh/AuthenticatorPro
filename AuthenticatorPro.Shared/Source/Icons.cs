using System.Collections.Generic;
using System.Linq;

namespace AuthenticatorPro.Shared
{
    public static class Icons
    {
        public static readonly Dictionary<string, int> Service = new Dictionary<string, int> 
        {
            {"1and1", Resource.Drawable.auth_1and1},
            {"3cx", Resource.Drawable.auth_3cx},
            {"adobe", Resource.Drawable.auth_adobe},
            {"algolia", Resource.Drawable.auth_algolia},
            {"amazon", Resource.Drawable.auth_amazon},
            {"appveyor", Resource.Drawable.auth_appveyor},
            {"aws", Resource.Drawable.auth_aws},
            {"azure", Resource.Drawable.auth_azure},
            {"backblaze", Resource.Drawable.auth_backblaze},
            {"battlenet", Resource.Drawable.auth_battlenet},
            {"binance", Resource.Drawable.auth_binance},
            {"bitbucket", Resource.Drawable.auth_bitbucket},
            {"bitcoin", Resource.Drawable.auth_bitcoin},
            {"bitfinex", Resource.Drawable.auth_bitfinex},
            {"bitwarden", Resource.Drawable.auth_bitwarden},
            {"blockchain", Resource.Drawable.auth_blockchain},
            {"cexio", Resource.Drawable.auth_cexio},
            {"coinbase", Resource.Drawable.auth_coinbase},
            {"default", Resource.Drawable.auth_default},
            {"digitalocean", Resource.Drawable.auth_digitalocean},
            {"discord", Resource.Drawable.auth_discord},
            {"dropbox", Resource.Drawable.auth_dropbox},
            {"electronicarts", Resource.Drawable.auth_electronicarts},
            {"epicgames", Resource.Drawable.auth_epicgames},
            {"evernote", Resource.Drawable.auth_evernote},
            {"facebook", Resource.Drawable.auth_facebook},
            {"figma", Resource.Drawable.auth_figma},
            {"firefox", Resource.Drawable.auth_firefox},
            {"gandi", Resource.Drawable.auth_gandi},
            {"github", Resource.Drawable.auth_github},
            {"gitlab", Resource.Drawable.auth_gitlab},
            {"gmail", Resource.Drawable.auth_gmail},
            {"godaddy", Resource.Drawable.auth_godaddy},
            {"google", Resource.Drawable.auth_google},
            {"googlecloudplatform", Resource.Drawable.auth_googlecloudplatform},
            {"googledrive", Resource.Drawable.auth_googledrive},
            {"googleplay", Resource.Drawable.auth_googleplay},
            {"guildwars2", Resource.Drawable.auth_guildwars2},
            {"hangouts", Resource.Drawable.auth_hangouts},
            {"heroku", Resource.Drawable.auth_heroku},
            {"humblebundle", Resource.Drawable.auth_humblebundle},
            {"ifttt", Resource.Drawable.auth_ifttt},
            {"instagram", Resource.Drawable.auth_instagram},
            {"jetbrains", Resource.Drawable.auth_jetbrains},
            {"jottacloud", Resource.Drawable.auth_jottacloud},
            {"kickstarter", Resource.Drawable.auth_kickstarter},
            {"kraken", Resource.Drawable.auth_kraken},
            {"lastpass", Resource.Drawable.auth_lastpass},
            {"linkedin", Resource.Drawable.auth_linkedin},
            {"logmein", Resource.Drawable.auth_logmein},
            {"mailchimp", Resource.Drawable.auth_mailchimp},
            {"mega", Resource.Drawable.auth_mega},
            {"microsoft", Resource.Drawable.auth_microsoft},
            {"namecheap", Resource.Drawable.auth_namecheap},
            {"nextcloud", Resource.Drawable.auth_nextcloud},
            {"nintendo", Resource.Drawable.auth_nintendo},
            {"npm", Resource.Drawable.auth_npm},
            {"office365", Resource.Drawable.auth_office365},
            {"onedrive", Resource.Drawable.auth_onedrive},
            {"origin", Resource.Drawable.auth_origin},
            {"outlook", Resource.Drawable.auth_outlook},
            {"ovh", Resource.Drawable.auth_ovh},
            {"paypal", Resource.Drawable.auth_paypal},
            {"pluralsight", Resource.Drawable.auth_pluralsight},
            {"privateinternetaccess", Resource.Drawable.auth_privateinternetaccess},
            {"protonmail", Resource.Drawable.auth_protonmail},
            {"protonvpn", Resource.Drawable.auth_protonvpn},
            {"reddit", Resource.Drawable.auth_reddit},
            {"rockstargames", Resource.Drawable.auth_rockstargames},
            {"samsung", Resource.Drawable.auth_samsung},
            {"scaleway", Resource.Drawable.auth_scaleway},
            {"skype", Resource.Drawable.auth_skype},
            {"slack", Resource.Drawable.auth_slack},
            {"snapchat", Resource.Drawable.auth_snapchat},
            {"teamviewer", Resource.Drawable.auth_teamviewer},
            {"trello", Resource.Drawable.auth_trello},
            {"tutanota", Resource.Drawable.auth_tutanota},
            {"twitch", Resource.Drawable.auth_twitch},
            {"twitter", Resource.Drawable.auth_twitter},
            {"uber", Resource.Drawable.auth_uber},
            {"ubisoft", Resource.Drawable.auth_ubisoft},
            {"uplay", Resource.Drawable.auth_uplay},
            {"uptimerobot", Resource.Drawable.auth_uptimerobot},
            {"wargaming", Resource.Drawable.auth_wargaming},
            {"wordpress", Resource.Drawable.auth_wordpress},
            {"xbox", Resource.Drawable.auth_xbox},
            {"youtube", Resource.Drawable.auth_youtube}
        };

        private static readonly Dictionary<string, int> ServiceDark = new Dictionary<string, int>
        {
            {"adobe", Resource.Drawable.auth_adobe_dark},
            {"amazon", Resource.Drawable.auth_amazon_dark},
            {"default", Resource.Drawable.auth_default_dark},
            {"discord", Resource.Drawable.auth_discord_dark},
            {"electronicarts", Resource.Drawable.auth_electronicarts_dark},
            {"github", Resource.Drawable.auth_github_dark},
            {"heroku", Resource.Drawable.auth_heroku_dark},
            {"ifttt", Resource.Drawable.auth_ifttt_dark},
            {"instagram", Resource.Drawable.auth_instagram_dark},
            {"jetbrains", Resource.Drawable.auth_jetbrains_dark},
            {"logmein", Resource.Drawable.auth_logmein_dark},
            {"protonvpn", Resource.Drawable.auth_protonvpn_dark},
            {"rockstargames", Resource.Drawable.auth_rockstargames_dark},
            {"uber", Resource.Drawable.auth_uber_dark},
            {"ubisoft", Resource.Drawable.auth_ubisoft_dark},
            {"wordpress", Resource.Drawable.auth_wordpress_dark},
            {"xbox", Resource.Drawable.auth_xbox_dark}
        };

        public static int GetService(string key, bool isDark)
        {
            if(isDark)
            {
                if(key == null)
                    return ServiceDark["default"];

                if(ServiceDark.ContainsKey(key))
                    return ServiceDark[key];

                return Service.ContainsKey(key) ? Service[key] : ServiceDark["default"];
            }

            if(key == null)
                return Service["default"];

            return Service.ContainsKey(key) ? Service[key] : Service["default"];
        }

        public static string FindServiceKeyByName(string name)
        {
            var key = name.ToLower().Split(' ')[0];
            return Service.Keys.Contains(key) ? key : "default";
        }
    }
}