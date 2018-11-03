using System.Collections.Generic;

namespace PlusAuth.Utilities
{
    internal static class Icon
    {
        public static Dictionary<string, int> List = new Dictionary<string, int>
        {
            { "default", Resource.Drawable.auth_default },
            { "google", Resource.Drawable.auth_google },
            { "facebook", Resource.Drawable.auth_facebook }
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