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
            { "proauth", Resource.Mipmap.ic_launcher }
        };

        public static readonly Dictionary<string, int> DarkIcons = new Dictionary<string, int>
        {
            { "up", Resource.Drawable.ic_arrow_upward_dark },
            { "folder", Resource.Drawable.ic_folder_dark },
            { "file", Resource.Drawable.ic_insert_drive_file_dark },
            { "proauth", Resource.Mipmap.ic_launcher }
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
            { "ovh", Resource.Drawable.auth_ovh }
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