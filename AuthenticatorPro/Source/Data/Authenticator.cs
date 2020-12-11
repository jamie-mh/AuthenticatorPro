using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AuthenticatorPro.Data.Generator;
using AuthenticatorPro.Util;
using Newtonsoft.Json;
using OtpNet;
using SQLite;
using Hotp = AuthenticatorPro.Data.Generator.Hotp;
using Totp = AuthenticatorPro.Data.Generator.Totp;

namespace AuthenticatorPro.Data
{
    [Table("authenticator")]
    public class Authenticator
    {
        public const int IssuerMaxLength = 32;
        public const int UsernameMaxLength = 40;

        public const OtpHashMode DefaultAlgorithm = OtpHashMode.Sha1;
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

        private IGenerator _generator;
        private long _lastCounter;
        private string _code;


        public Authenticator()
        {
            TimeRenew = DateTime.MinValue;
            _code = null;
            _generator = null;
            _lastCounter = 0;

            Algorithm = DefaultAlgorithm;
            Digits = DefaultDigits;
            Period = DefaultPeriod;
        }

        public string GetCode()
        {
            _generator ??= Type switch
            {
                AuthenticatorType.Totp => new Totp(Secret, Period, Algorithm, Digits),
                AuthenticatorType.Hotp => new Hotp(Secret, Algorithm, Digits, Counter),
                AuthenticatorType.MobileOtp => new MobileOtp(Secret, Digits, Period),
                _ => throw new ArgumentException("Unknown authenticator type.")
            };
            
            if(_generator is ICounterBasedGenerator counterGenerator)
            {
                if(_lastCounter == Counter)
                    return _code;
                
                counterGenerator.Counter = Counter;
                var oldCode = _code;
                _code = counterGenerator.Compute();
                _lastCounter = Counter;
                
                if(oldCode != null || Counter == 1)
                    TimeRenew = counterGenerator.GetRenewTime();
            }
            else if(TimeRenew <= DateTime.Now)
            {
                _code = _generator.Compute();
                TimeRenew = _generator.GetRenewTime();
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
                issuer = input.Username.Trim().Truncate(IssuerMaxLength);
                username = null;
            }
            else
            {
                issuer = input.Issuer.Trim().Truncate(IssuerMaxLength);
                // For some odd reason the username field always follows a '[issuer]: [username]' format
                username = input.Username.Replace($"{input.Issuer}: ", "").Trim().Truncate(UsernameMaxLength);
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
                secret = CleanSecret(secret, type);
            }
            catch
            {
                throw new ArgumentException();
            }

            var auth = new Authenticator
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
                _ => throw new ArgumentException("Unknown type")
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

            var algorithm = DefaultAlgorithm;

            if(args.ContainsKey("algorithm"))
                algorithm = args["algorithm"].ToUpper() switch
                {
                    "SHA1" => OtpHashMode.Sha1,
                    "SHA256" => OtpHashMode.Sha256,
                    "SHA512" => OtpHashMode.Sha512,
                    _ => throw new ArgumentException("Unknown algorithm")
                };

            var digits = DefaultDigits;
            if(args.ContainsKey("digits") && !Int32.TryParse(args["digits"], out digits))
                throw new ArgumentException("Digits parameter cannot be parsed.");
            
            var period = DefaultPeriod;
            if(args.ContainsKey("period") && !Int32.TryParse(args["period"], out period))
                throw new ArgumentException("Period parameter cannot be parsed.");

            var counter = 0;
            if(type == AuthenticatorType.Hotp && args.ContainsKey("counter") && !Int32.TryParse(args["counter"], out counter))
                throw new ArgumentException("Counter parameter cannot be parsed.");
            
            if(counter < 0)
                throw new ArgumentException("Counter cannot be negative.");

            if(!args.ContainsKey("secret"))
                throw new ArgumentException("Secret parameter is required.");
            
            var secret = CleanSecret(args["secret"], type);

            var auth = new Authenticator {
                Secret = secret,
                Issuer = issuer.Trim().Truncate(IssuerMaxLength),
                Username = username?.Trim().Truncate(UsernameMaxLength),
                Icon = Shared.Data.Icon.FindServiceKeyByName(issuer),
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = counter
            };

            if(!auth.IsValid())
                throw new ArgumentException("Authenticator is not valid.");
            
            return auth;
        }
        
        public string GetOtpAuthUri()
        {
            var type = Type switch
            {
                AuthenticatorType.Hotp => "hotp",
                AuthenticatorType.Totp => "totp",
                _ => throw new ArgumentException("Unknown type")
            };
            
            var issuerUsername = String.IsNullOrEmpty(Username) ? Issuer : $"{Issuer}:{Username}";

            var uri = new StringBuilder(
                $"otpauth://{type}/{Uri.EscapeDataString(issuerUsername)}?secret={Secret}&issuer={Uri.EscapeDataString(Issuer)}");

            if(Algorithm != DefaultAlgorithm)
            {
                var algorithmName = Algorithm switch
                {
                    OtpHashMode.Sha1 => "SHA1",
                    OtpHashMode.Sha256 => "SHA256",
                    OtpHashMode.Sha512 => "SHA512",
                    _ => throw new ArgumentException("Unknown algorithm")
                };

                uri.Append($"&algorithm={algorithmName}");
            }

            if(Digits != DefaultDigits)
                uri.Append($"&digits={Digits}");
            
            if(Type == AuthenticatorType.Totp && Period != DefaultPeriod)
                uri.Append($"&period={Period}");
            
            if(Type == AuthenticatorType.Hotp)
                uri.Append($"&counter={Counter}");

            return uri.ToString();
        }

        public static string CleanSecret(string input, AuthenticatorType type)
        {
            if(type == AuthenticatorType.Totp || type == AuthenticatorType.Hotp)
                input = input.ToUpper();
            
            input = input.Replace(" ", "");
            input = input.Replace("-", "");

            return input;
        }

        public static bool IsValidSecret(string secret, AuthenticatorType type)
        {
            if(String.IsNullOrEmpty(secret))
                return false;

            switch(type)
            {
                case AuthenticatorType.Totp:
                case AuthenticatorType.Hotp:
                    try
                    {
                        return Base32Encoding.ToBytes(secret).Length > 0;
                    }
                    catch(ArgumentException)
                    {
                        return false;
                    }
                    
                case AuthenticatorType.MobileOtp:
                    return secret.Length >= MobileOtp.SecretMinLength;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public bool IsValid()
        {
            return !String.IsNullOrEmpty(Issuer) && IsValidSecret(Secret, Type) && Digits >= MinDigits && Digits <= MaxDigits && Period > 0;
        }
    }
}