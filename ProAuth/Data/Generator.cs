using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OtpSharp;
using SQLite;

namespace ProAuth.Data
{
    [Table("generator")]
    class Generator
    {
        [Column("id"), PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("type")]
        public OtpType Type { get; set; }

        [Column("issuer")]
        public string Issuer { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("secret"), MaxLength(32)]
        public string Secret { get; set; }

        [Column("algorithm")]
        public OtpHashMode Algorithm { get; set; }

        [Column("digits")]
        public int Digits { get; set; }

        [Column("period")]
        public int Period { get; set; }

        [Column("counter")]
        public long Counter { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }

        [Column("start")]
        public DateTime TimeRenew { get; set; }

        [Column("code")]
        public string Code { get; set; }

        public static Generator FromKeyUri(string uri)
        {
            const string uriExpr = @"^otpauth:\/\/([a-z]+)\/(.*?)\?(.*?)$";
            string raw = Uri.UnescapeDataString(uri);
            Match uriMatch = Regex.Match(raw, uriExpr);

            if(!uriMatch.Success)
            {
                return null;
            }

            OtpType type = (uriMatch.Groups[1].Value == "totp") ? OtpType.Totp : OtpType.Hotp;

            string issuer = "";
            string username = "";

            // Get the issuer and username if possible
            const string issuerNameExpr = @"^(.*?):(.*?)$";
            Match issuerName = Regex.Match(uriMatch.Groups[2].Value, issuerNameExpr);

            if(issuerName.Success)
            {
                issuer = issuerName.Groups[1].Value;
                username = issuerName.Groups[2].Value;
            }
            else
            {
                issuer = uriMatch.Groups[2].Value;
            }

            string queryString = uriMatch.Groups[3].Value;
            Dictionary<string, string> args = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?").Cast<Match>()
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);

            OtpHashMode algorithm = OtpHashMode.Sha1;
            if(args.ContainsKey("algorithm"))
            {
                switch(args["algorithm"].ToUpper())
                {
                    case "SHA256":
                        algorithm = OtpHashMode.Sha256;
                        break;

                    case "SHA512":
                        algorithm = OtpHashMode.Sha512;
                        break;
                }
            }

            int digits = (args.ContainsKey("digits")) ? Int32.Parse(args["digits"]) : 6;

            // todo include counter

            int period = (args.ContainsKey("period")) ? Int32.Parse(args["period"]) : 30;

            Generator gen = new Generator
            {
                Secret = args["secret"],
                Issuer = issuer,
                Username = username,
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                TimeRenew = DateTime.MinValue,
                Code = ""
            };

            return gen;
        }
    }
}