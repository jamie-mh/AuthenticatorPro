using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Android.Service.Notification;
using Android.Widget;
using Newtonsoft.Json;
using OtpSharp;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth.Data
{
    [Table("authenticator")]
    internal class Authenticator
    {
        [Column("type")]
        public OtpType Type { get; set; }

        [Column("createdDate")]
        public DateTime CreatedDate { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("issuer"), MaxLength(32)]
        public string Issuer { get; set; }

        [Column("username"), MaxLength(32)]
        public string Username { get; set; }

        [Column("secret"), PrimaryKey, MaxLength(32)]
        public string Secret { get; set; }

        [Column("algorithm")]
        public OtpHashMode Algorithm { get; set; }

        [Column("digits")]
        public int Digits { get; set; }

        [Column("period")]
        public int Period { get; set; }

        [Column("counter")]
        public long Counter { get; set; }

        [Column("ranking"), JsonIgnore]
        public int Ranking { get; set; }

        [JsonIgnore]
        public DateTime TimeRenew { get; set; }

        [JsonIgnore]
        public string Code { get; set; }

        public Authenticator()
        {
            Code = "";
            TimeRenew = DateTime.Now;
            CreatedDate = DateTime.Now;
            Ranking = 100000;
        }

        public static Authenticator FromKeyUri(string uri)
        {
            const string uriExpr = @"^otpauth:\/\/([a-z]+)\/(.*?)\?(.*?)$";
            string raw = Uri.UnescapeDataString(uri);
            Match uriMatch = Regex.Match(raw, uriExpr);

            if(!uriMatch.Success)
            {
                throw new InvalidFormatException();
            }

            OtpType type = (uriMatch.Groups[1].Value == "totp") ? OtpType.Totp : OtpType.Hotp;

            // Get the issuer and username if possible
            const string issuerNameExpr = @"^(.*?):(.*?)$";
            Match issuerName = Regex.Match(uriMatch.Groups[2].Value, issuerNameExpr);

            string issuer;
            string username;

            if(issuerName.Success)
            {
                issuer = issuerName.Groups[1].Value;
                username = issuerName.Groups[2].Value;
            }
            else
            {
                issuer = uriMatch.Groups[2].Value;
                username = "";
            }

            string queryString = uriMatch.Groups[3].Value;
            Dictionary<string, string> args = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?").Cast<Match>()
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);

            OtpHashMode algorithm = OtpHashMode.Sha1;

            if(args.ContainsKey("algorithm"))
            {
                switch(args["algorithm"].ToUpper())
                {
                    case "SHA1":
                        algorithm = OtpHashMode.Sha1;
                        break;

                    case "SHA256":
                        algorithm = OtpHashMode.Sha256;
                        break;

                    case "SHA512":
                        algorithm = OtpHashMode.Sha512;
                        break;

                    default:
                        throw new InvalidFormatException();
                }
            }

            int digits = (args.ContainsKey("digits")) ? Int32.Parse(args["digits"]) : 6;
            int period = (args.ContainsKey("period")) ? Int32.Parse(args["period"]) : 30;

            string code = "";
            for(int i = 0; i < digits; code += "-", i++);

            Authenticator auth = new Authenticator
            {
                Secret = args["secret"].ToUpper(),
                Issuer = issuer.Trim().Truncate(32),
                Username = username.Trim().Truncate(32),
                Icon = Icons.FindServiceKeyByName(issuer),
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = 0,
                Code = code
            };

            return auth;
        }
    }

    internal class InvalidFormatException : Exception
    {

    }
}