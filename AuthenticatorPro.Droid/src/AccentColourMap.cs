using System;

namespace AuthenticatorPro.Droid
{
    internal static class AccentColourMap
    {
        public static int GetOverlay(string name)
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
    }
}