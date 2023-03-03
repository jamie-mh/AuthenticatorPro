// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AuthenticatorPro.Core
{
    public static class UriParser
    {
        private static UriParseResult ParseMotpUri(string uri, IIconResolver iconResolver)
        {
            var match = Regex.Match(Uri.UnescapeDataString(uri), @"^motp:\/\/(.*?):(.*?)\?secret=([a-fA-F\d]+)$");

            if (!match.Success || match.Groups.Count < 4)
            {
                throw new ArgumentException("URI is not a valid mOTP");
            }

            var issuer = match.Groups[1].Value;
            var icon = iconResolver.FindServiceKeyByName(issuer);

            var auth = new Authenticator
            {
                Type = AuthenticatorType.MobileOtp,
                Issuer = issuer,
                Icon = icon,
                Username = match.Groups[2].Value,
                Secret = match.Groups[3].Value,
                Digits = MobileOtp.Digits,
                Period = AuthenticatorType.MobileOtp.GetDefaultPeriod()
            };

            return new UriParseResult { Authenticator = auth, PinLength = MobileOtp.PinLength };
        }

        private static UriParseResult ParseOtpAuthUri(string uri, IIconResolver iconResolver)
        {
            var uriMatch = Regex.Match(Uri.UnescapeDataString(uri), @"^otpauth:\/\/([a-z]+)\/([^?]*)(.*)$");

            if (!uriMatch.Success)
            {
                throw new ArgumentException("URI is not a valid otpauth");
            }

            var queryString = uriMatch.Groups[3].Value;

            var argMatches = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?");
            var args = new Dictionary<string, string>();

            foreach (Match match in argMatches)
            {
                if (!args.ContainsKey(match.Groups[1].Value))
                {
                    args.Add(match.Groups[1].Value, match.Groups[3].Value);
                }
            }

            // Get the issuer and username if possible
            var issuerUsername = uriMatch.Groups[2].Value;
            var issuerUsernameMatch = Regex.Match(issuerUsername, @"^(.*?):(.*)$");

            string issuer;
            string username;

            if (issuerUsernameMatch.Success)
            {
                var issuerValue = issuerUsernameMatch.Groups[1].Value;
                var usernameValue = issuerUsernameMatch.Groups[2].Value;

                if (issuerValue == "")
                {
                    issuer = usernameValue;
                    username = null;
                }
                else
                {
                    issuer = issuerValue;
                    username = usernameValue;
                }
            }
            else
            {
                if (args.ContainsKey("issuer"))
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

            var type = uriMatch.Groups[1].Value switch
            {
                "totp" when issuer == "Steam" || args.ContainsKey("steam") => AuthenticatorType.SteamOtp,
                "totp" => AuthenticatorType.Totp,
                "hotp" => AuthenticatorType.Hotp,
                "yaotp" => AuthenticatorType.YandexOtp,
                _ => throw new ArgumentException("Unknown type")
            };

            if (type == AuthenticatorType.YandexOtp)
            {
                issuer = "Yandex";
                username = issuerUsername;
            }

            var algorithm = Authenticator.DefaultAlgorithm;

            if (args.ContainsKey("algorithm") && type.HasVariableAlgorithm())
            {
                algorithm = args["algorithm"].ToUpper() switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException("Unknown algorithm")
                };
            }

            var digits = type.GetDefaultDigits();
            var hasVariableDigits = type.GetMinDigits() != type.GetMaxDigits();

            if (hasVariableDigits && args.ContainsKey("digits") && !Int32.TryParse(args["digits"], out digits))
            {
                throw new ArgumentException("Digits parameter cannot be parsed");
            }

            var period = type.GetDefaultPeriod();

            if (type.HasVariablePeriod() && args.ContainsKey("period") && !Int32.TryParse(args["period"], out period))
            {
                throw new ArgumentException("Period parameter cannot be parsed");
            }

            var counter = 0;
            if (type == AuthenticatorType.Hotp && args.ContainsKey("counter") &&
                !Int32.TryParse(args["counter"], out counter))
            {
                throw new ArgumentException("Counter parameter cannot be parsed");
            }

            if (counter < 0)
            {
                throw new ArgumentException("Counter cannot be negative");
            }

            if (!args.ContainsKey("secret"))
            {
                throw new ArgumentException("Secret parameter is required");
            }

            var icon = iconResolver.FindServiceKeyByName(args.ContainsKey("icon") ? args["icon"] : issuer);
            var secret = SecretUtil.Clean(args["secret"], type);

            var pinLength = 0;

            if (type == AuthenticatorType.YandexOtp && args.ContainsKey("pin_length") &&
                !Int32.TryParse(args["pin_length"], out pinLength))
            {
                throw new ArgumentException("Pin length parameter cannot be parsed");
            }

            var auth = new Authenticator
            {
                Secret = secret,
                Issuer = issuer.Trim().Truncate(Authenticator.IssuerMaxLength),
                Username = username?.Trim().Truncate(Authenticator.UsernameMaxLength),
                Icon = icon,
                Type = type,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = counter
            };

            auth.Validate();
            return new UriParseResult { Authenticator = auth, PinLength = pinLength };
        }

        public static UriParseResult ParseStandardUri(string uri, IIconResolver iconResolver)
        {
            if (uri.StartsWith("otpauth"))
            {
                return ParseOtpAuthUri(uri, iconResolver);
            }

            if (uri.StartsWith("motp"))
            {
                return ParseMotpUri(uri, iconResolver);
            }

            throw new ArgumentException("Unknown URI scheme");
        }

        public static OtpAuthMigration ParseOtpAuthMigrationUri(string uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var real = Uri.UnescapeDataString(uri);
            var match = Regex.Match(real, @"^otpauth-migration:\/\/offline\?data=(.*)$");

            if (!match.Success)
            {
                throw new ArgumentException("Invalid URI");
            }

            var rawData = match.Groups[1].Value;

            if (rawData.Length % 4 != 0)
            {
                var nextFactor = (rawData.Length + 4 - 1) / 4 * 4;
                rawData = rawData.PadRight(nextFactor, '=');
            }

            ReadOnlySpan<byte> protoMessage = Convert.FromBase64String(rawData);
            var migration = Serializer.Deserialize<OtpAuthMigration>(protoMessage);

            return migration;
        }
    }
}