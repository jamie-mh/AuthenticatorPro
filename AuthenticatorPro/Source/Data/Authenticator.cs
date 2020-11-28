using System;
using System.Linq;
using System.Text.RegularExpressions;
using AuthenticatorPro.Util;
using Newtonsoft.Json;
using OtpNet;
using SQLite;

namespace AuthenticatorPro.Data
{
    [Table("authenticator")]
    internal class Authenticator
    {
        public const int IssuerMaxLength = 32;
        public const int UsernameMaxLength = 40;
        
        public const int DefaultDigits = 6;
        public const int DefaultPeriod = 30;

        public const int MinDigits = 6;
        public const int MaxDigits = 10;


        [Column("type")]
        public AuthenticatorType Type { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("issuer")]
        [MaxLength(IssuerMaxLength)]
        public string Issuer { get; set; }

        [Column("username")]
        [MaxLength(UsernameMaxLength)]
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

                if(Type == AuthenticatorType.Hotp)
                    _otp = new Hotp(secret, Algorithm, Digits);
                else if(Type == AuthenticatorType.Totp)
                    _otp = new Totp(secret, Period, Algorithm, Digits);
            }

            if(Type == AuthenticatorType.Totp && TimeRenew <= DateTime.Now)
            {
                var totp = (Totp) _otp;
                _code = totp.ComputeTotp();
                TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());
            }
            else if(Type == AuthenticatorType.Hotp && _lastCounter != Counter)
            {
                var hotp = (Hotp) _otp;

                if(_code != null)
                    TimeRenew = DateTime.Now.AddSeconds(10);

                _code = hotp.ComputeHOTP(Counter);
                _lastCounter = Counter;
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
                _ => throw new ArgumentException()
            };

            var algorithm = input.Algorithm switch
            {
                OtpAuthMigration.Algorithm.Sha1 => OtpHashMode.Sha1,
                _ => throw new ArgumentException()
            };

            string secret;

            try
            {
                secret = Base32Encoding.ToString(input.Secret);
                secret = CleanSecret(secret);
            }
            catch
            {
                throw new ArgumentException();
            }

            var auth = new Authenticator()
            {
                Issuer = issuer,
                Username = username,
                Algorithm = algorithm,
                Type = type,
                Secret = secret,
                Counter = input.Counter,
                Digits = DefaultDigits,
                Period = DefaultPeriod,
                Icon = Shared.Data.Icon.FindServiceKeyByName(issuer)
            };
            
            if(!auth.IsValid())
                throw new ArgumentException();
            
            return auth;
        }

        public static Authenticator FromOtpAuthUri(string uri)
        {
            var uriMatch = Regex.Match(Uri.UnescapeDataString(uri), @"^otpauth:\/\/([a-z]+)\/(.*)\?(.*)$");

            if(!uriMatch.Success)
                throw new ArgumentException("URI is not valid");

            var type = uriMatch.Groups[1].Value switch {
                "totp" => AuthenticatorType.Totp,
                "hotp" => AuthenticatorType.Hotp,
                _ => throw new ArgumentException()
            };

            // Get the issuer and username if possible
            var issuerUsername = uriMatch.Groups[2].Value;
            var issuerUsernameMatch = Regex.Match(issuerUsername, @"^(.*):(.*)$");
            
            var queryString = uriMatch.Groups[3].Value;
            var args = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?")
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);

            string issuer;
            string username;

            if(issuerUsernameMatch.Success)
            {
                issuer = issuerUsernameMatch.Groups[1].Value;
                username = issuerUsernameMatch.Groups[2].Value;
            }
            else
            {
                if(args.ContainsKey("issuer"))
                {
                    issuer = args["issuer"];
                    username = issuerUsername;
                }
                else
                {
                    issuer = uriMatch.Groups[2].Value;
                    username = null;
                }
            }

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
                        throw new ArgumentException();
                }

            var digits = args.ContainsKey("digits") ? Int32.Parse(args["digits"]) : DefaultDigits;
            var period = args.ContainsKey("period") ? Int32.Parse(args["period"]) : DefaultPeriod;

            var secret = CleanSecret(args["secret"]);

            var auth = new Authenticator {
                Secret = secret,
                Issuer = issuer.Trim().Truncate(IssuerMaxLength),
                Username = username.Trim().Truncate(UsernameMaxLength),
                Icon = Shared.Data.Icon.FindServiceKeyByName(issuer),
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = 0
            };

            if(!auth.IsValid())
                throw new ArgumentException();
            
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

        public bool IsValid()
        {
            return !String.IsNullOrEmpty(Issuer) && IsValidSecret(Secret) && Digits >= MinDigits && Digits <= MaxDigits && Period > 0;
        }
    }
}