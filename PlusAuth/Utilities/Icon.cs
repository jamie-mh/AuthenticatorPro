using System.Collections.Generic;

namespace PlusAuth.Utilities
{
    internal static class Icon
    {
        public static Dictionary<string, int> List = new Dictionary<string, int>
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
            { "ovh", Resource.Drawable.auth_ovh }
        };

        public static int Get(string key)
        {
            if(key == null || !List.ContainsKey(key))
            {
                return List["default"];
            }

            return List[key];
        }
    }
}