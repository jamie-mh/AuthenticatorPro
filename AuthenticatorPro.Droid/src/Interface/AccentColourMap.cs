using System;

namespace AuthenticatorPro.Droid.Interface
{
    internal static class AccentColourMap
    {
        public static int GetOverlayId(string name)
        {
            return name switch
            {
                "red" => Resource.Style.OverlayAccentRed,
                "pink" => Resource.Style.OverlayAccentPink,
                "purple" => Resource.Style.OverlayAccentPurple,
                "deepPurple" => Resource.Style.OverlayAccentDeepPurple,
                "indigo" => Resource.Style.OverlayAccentIndigo,
                "blue" => Resource.Style.OverlayAccentBlue,
                "lightBlue" => Resource.Style.OverlayAccentLightBlue,
                "cyan" => Resource.Style.OverlayAccentCyan,
                "teal" => Resource.Style.OverlayAccentTeal,
                "green" => Resource.Style.OverlayAccentGreen,
                "lightGreen" => Resource.Style.OverlayAccentLightGreen,
                "lime" => Resource.Style.OverlayAccentLime,
                "yellow" => Resource.Style.OverlayAccentYellow,
                "amber" => Resource.Style.OverlayAccentAmber,
                "orange" => Resource.Style.OverlayAccentOrange,
                "deepOrange" => Resource.Style.OverlayAccentDeepOrange,
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }

        public static int GetColourId(string name)
        {
            return name switch
            {
                "red" => Resource.Color.md_theme_light_red_primary,
                "pink" => Resource.Color.md_theme_light_pink_primary,
                "purple" => Resource.Color.md_theme_light_purple_primary,
                "deepPurple" => Resource.Color.md_theme_light_deepPurple_primary,
                "indigo" => Resource.Color.md_theme_light_indigo_primary,
                "blue" => Resource.Color.md_theme_light_blue_primary,
                "lightBlue" => Resource.Color.md_theme_light_lightBlue_primary,
                "cyan" => Resource.Color.md_theme_light_cyan_primary,
                "teal" => Resource.Color.md_theme_light_teal_primary,
                "green" => Resource.Color.md_theme_light_green_primary,
                "lightGreen" => Resource.Color.md_theme_dark_lightGreen_primary,
                "lime" => Resource.Color.md_theme_light_lime_primary,
                "yellow" => Resource.Color.md_theme_light_yellow_primary,
                "amber" => Resource.Color.md_theme_light_amber_primary,
                "orange" => Resource.Color.md_theme_light_orange_primary,
                "deepOrange" => Resource.Color.md_theme_light_deepOrange_primary,
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }
    }
}