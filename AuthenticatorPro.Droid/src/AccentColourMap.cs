using System;

namespace AuthenticatorPro.Droid
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
                "red" => Resource.Color.colorRedPrimary,
                "pink" => Resource.Color.colorPinkPrimary,
                "purple" => Resource.Color.colorPurplePrimary,
                "deepPurple" => Resource.Color.colorDeepPurplePrimary,
                "indigo" => Resource.Color.colorIndigoPrimary,
                "blue" => Resource.Color.colorBluePrimary,
                "lightBlue" => Resource.Color.colorLightBluePrimary,
                "cyan" => Resource.Color.colorCyanPrimary,
                "teal" => Resource.Color.colorTealPrimary,
                "green" => Resource.Color.colorGreenPrimary,
                "lightGreen" => Resource.Color.colorLightGreenPrimary,
                "lime" => Resource.Color.colorLimePrimary,
                "yellow" => Resource.Color.colorYellowPrimary,
                "amber" => Resource.Color.colorAmberPrimary,
                "orange" => Resource.Color.colorOrangePrimary,
                "deepOrange" => Resource.Color.colorDeepOrangePrimary,
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }
    }
}