using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OtpSharp;
using SQLite;

namespace AuthenticatorPro.Data
{
    [Table("authenticator")]
    internal class Authenticator : IAuthenticatorInfo
    {
        public Authenticator()
        {
            Code = "";
            TimeRenew = DateTime.Now;
            Ranking = 1;
        }

        [JsonIgnore] public string Code { get; set; }

        [Column("type")] public OtpType Type { get; set; }

        [Column("icon")] public string Icon { get; set; }

        [Column("issuer")] [MaxLength(32)] public string Issuer { get; set; }

        [Column("username")] [MaxLength(32)] public string Username { get; set; }

        [Column("secret")]
        [PrimaryKey]
        [MaxLength(32)]
        public string Secret { get; set; }

        [Column("algorithm")] public OtpHashMode Algorithm { get; set; }

        [Column("digits")] public int Digits { get; set; }

        [Column("period")] public int Period { get; set; }

        [Column("counter")] public long Counter { get; set; }

        [Column("ranking")] public int Ranking { get; set; }

        [JsonIgnore] public DateTime TimeRenew { get; set; }

        public static Authenticator FromKeyUri(string uri)
        {
            const string uriExpr = @"^otpauth:\/\/([a-z]+)\/(.*?)\?(.*?)$";
            var raw = Uri.UnescapeDataString(uri);
            var uriMatch = Regex.Match(raw, uriExpr);

            if(!uriMatch.Success)
                throw new InvalidFormatException();

            var type = uriMatch.Groups[1].Value == "totp" ? OtpType.Totp : OtpType.Hotp;

            // Get the issuer and username if possible
            const string issuerNameExpr = @"^(.*?):(.*?)$";
            var issuerName = Regex.Match(uriMatch.Groups[2].Value, issuerNameExpr);

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

            var queryString = uriMatch.Groups[3].Value;
            var args = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?")
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);

            var algorithm = OtpHashMode.Sha1;

            if(args.ContainsKey("algorithm"))
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

            var digits = args.ContainsKey("digits") ? Int32.Parse(args["digits"]) : 6;
            var period = args.ContainsKey("period") ? Int32.Parse(args["period"]) : 30;

            var code = "";
            for(var i = 0; i < digits; code += "-", i++);

            var secret = args["secret"].ToUpper();

            if(!IsValidSecret(secret))
                throw new InvalidFormatException();

            var auth = new Authenticator {
                Secret = secret,
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

        public static bool IsValidSecret(string secret)
        {
            const string base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            return secret.ToCharArray().All(x => base32Alphabet.IndexOf(x) >= 0 || x == '=');
        }
    }

    internal class InvalidFormatException : Exception
    {
    }
}