using System;
using System.Linq;
using System.Text.RegularExpressions;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Util;
using Newtonsoft.Json;
using OtpNet;
using SQLite;

namespace AuthenticatorPro.Data
{
    [Table("authenticator")]
    internal class Authenticator
    {
        [Column("type")]
        public AuthenticatorType Type { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("issuer")]
        [MaxLength(32)]
        public string Issuer { get; set; }

        [Column("username")]
        [MaxLength(40)]
        public string Username{ get; set; }

        [Column("secret")]
        [PrimaryKey]
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

        [Ignore]
        [JsonIgnore]
        public DateTime TimeRenew { get; private set; }

        private Otp _otp;
        private long _lastCounter;
        private string _code;


        public Authenticator()
        {
            TimeRenew = DateTime.MinValue;
            _code = null;
            _otp = null;
        }

        public string GetCode()
        {
            if(_otp == null)
            {
                var secret = Base32Encoding.ToBytes(Secret);

                _otp = Type switch
                {
                    AuthenticatorType.Hotp => new Hotp(secret, Algorithm, Digits),
                    AuthenticatorType.Totp => new Totp(secret, Period, Algorithm, Digits)
                };
            }
            
            switch(Type)
            {
                case AuthenticatorType.Totp when TimeRenew <= DateTime.Now:
                {
                    var totp = (Totp) _otp;
                    _code = totp.ComputeTotp();
                    TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());
                    break;
                }
                case AuthenticatorType.Hotp when _lastCounter != Counter:
                {
                    var hotp = (Hotp) _otp;

                    if(_code != null)
                        TimeRenew = DateTime.Now.AddSeconds(10);

                    _code = hotp.ComputeHOTP(Counter);
                    _lastCounter = Counter;
                    break;
                }
            }

            return _code;
        }

        public static Authenticator FromOtpAuthMigrationAuthenticator(OtpAuthMigration.Authenticator input)
        {
            string issuer;
            string username;

            // Google Auth may not have an issuer, just use the username instead
            if(String.IsNullOrEmpty(input.Issuer))
            {
                issuer = input.Username.Trim().Truncate(32);
                username = null;
            }
            else
            {
                issuer = input.Issuer.Trim().Truncate(32);
                // For some odd reason the username field always follows a '[issuer]: [username]' format
                username = input.Username.Replace($"{input.Issuer}: ", "").Trim().Truncate(40);
            }

            var type = input.Type switch
            {
                OtpAuthMigration.Type.Totp => AuthenticatorType.Totp,
                OtpAuthMigration.Type.Hotp => AuthenticatorType.Hotp,
                _ => throw new InvalidAuthenticatorException()
            };

            var algorithm = input.Algorithm switch
            {
                OtpAuthMigration.Algorithm.Sha1 => OtpHashMode.Sha1,
                _ => throw new InvalidAuthenticatorException()
            };

            string secret;

            try
            {
                secret = Base32Encoding.ToString(input.Secret);
                secret = CleanSecret(secret);
            }
            catch
            {
                throw new InvalidAuthenticatorException();
            }

            var auth = new Authenticator()
            {
                Issuer = issuer,
                Username = username,
                Algorithm = algorithm,
                Type = type,
                Secret = secret,
                Counter = input.Counter,
                Digits = 6,
                Period = 30,
                Icon = Shared.Data.Icon.FindServiceKeyByName(issuer)
            };
            
            auth.Validate();
            return auth;
        }

        public static Authenticator FromOtpAuthUri(string uri)
        {
            const string uriExpr = @"^otpauth:\/\/([a-z]+)\/(.*?)\?(.*?)$";
            var raw = Uri.UnescapeDataString(uri);
            var uriMatch = Regex.Match(raw, uriExpr);

            if(!uriMatch.Success)
                throw new ArgumentException("URI is not valid");

            var type = uriMatch.Groups[1].Value switch {
                "totp" => AuthenticatorType.Totp,
                "hotp" => AuthenticatorType.Hotp,
                _ => throw new InvalidAuthenticatorException()
            };

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
                        throw new InvalidAuthenticatorException();
                }

            var digits = args.ContainsKey("digits") ? Int32.Parse(args["digits"]) : 6;
            var period = args.ContainsKey("period") ? Int32.Parse(args["period"]) : 30;

            var secret = CleanSecret(args["secret"]);

            var auth = new Authenticator {
                Secret = secret,
                Issuer = issuer.Trim().Truncate(32),
                Username = username.Trim().Truncate(40),
                Icon = Shared.Data.Icon.FindServiceKeyByName(issuer),
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = 0
            };

            auth.Validate();
            return auth;
        }

        public static string CleanSecret(string input)
        {
            input = input.ToUpper();
            input = input.Replace(" ", "");
            input = input.Replace("-", "");

            return input;
        }

        public static bool IsValidSecret(string secret)
        {
            if(String.IsNullOrEmpty(secret))
                return false;

            try
            {
                return Base32Encoding.ToBytes(secret).Length > 0;
            }
            catch(ArgumentException)
            {
                return false;
            }
        }

        public void Validate()
        {
            if(String.IsNullOrEmpty(Issuer) ||
               !IsValidSecret(Secret) || 
               Digits < 6 ||
               Digits > 10 ||
               Period <= 0)
                throw new InvalidAuthenticatorException();
        }
    }

    internal class InvalidAuthenticatorException : Exception
    {

    }
}