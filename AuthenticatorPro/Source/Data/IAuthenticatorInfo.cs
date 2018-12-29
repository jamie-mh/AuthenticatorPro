using System;
using OtpSharp;

namespace AuthenticatorPro.Data
{
    internal interface IAuthenticatorInfo
    {
        OtpType Type { get; set; }
        string Icon { get; set; }
        string Issuer { get; set; }
        string Username { get; set; }
        string Secret { get; set; }
        OtpHashMode Algorithm { get; set; }
        int Digits { get; set; }
        int Period { get; set; }
        long Counter { get; set; }
        int Ranking { get; set; }
        DateTime TimeRenew { get; set; }
    }
}