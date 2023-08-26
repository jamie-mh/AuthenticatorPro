// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using ProtoBuf;

namespace AuthenticatorPro.Core
{
    public static partial class UriParser
    {
        [GeneratedRegex(@"^otpauth-migration://offline\?data=(.*)$")]
        private static partial Regex OtpAuthMigrationRegex();

        [GeneratedRegex("([^?=&]+)(=([^&]*))?")]
        private static partial Regex QueryStringRegex();

        [GeneratedRegex(@"^otpauth://([a-z]+)/([^?]*)(.*)$")]
        private static partial Regex OtpAuthUriRegex();

        [GeneratedRegex("^(.*?):(.*)$")]
        private static partial Regex UsernameIssuerRegex();

        [GeneratedRegex(@"^motp://(.*?):(.*?)\?secret=([a-fA-F\d]+)$")]
        private static partial Regex MotpRegex();

        private static UriParseResult ParseMotpUri(string uri, IIconResolver iconResolver)
        {
            var match = MotpRegex().Match(Uri.UnescapeDataString(uri));

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
            var uriMatch = OtpAuthUriRegex().Match(Uri.UnescapeDataString(uri));

            if (!uriMatch.Success)
            {
                throw new ArgumentException("URI is not a valid otpauth");
            }

            var queryString = uriMatch.Groups[3].Value;

            var argMatches = QueryStringRegex().Matches(queryString);
            var args = new Dictionary<string, string>();

            foreach (var groups in argMatches.Select(m => m.Groups))
            {
                args.TryAdd(groups[1].Value, groups[3].Value);
            }

            // Get the issuer and username if possible
            var issuerUsername = uriMatch.Groups[2].Value;
            var issuerUsernameMatch = UsernameIssuerRegex().Match(issuerUsername);

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
                if (args.TryGetValue("issuer", out var issuerParam))
                {
                    issuer = issuerParam;
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

            if (args.TryGetValue("algorithm", out var algorithmParam) && type.HasVariableAlgorithm())
            {
                algorithm = algorithmParam.ToUpper() switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException("Unknown algorithm")
                };
            }

            var digits = type.GetDefaultDigits();
            var hasVariableDigits = type.GetMinDigits() != type.GetMaxDigits();

            if (hasVariableDigits && args.TryGetValue("digits", out var digitsParam) &&
                !int.TryParse(digitsParam, out digits))
            {
                throw new ArgumentException("Digits parameter cannot be parsed");
            }

            var period = type.GetDefaultPeriod();

            if (type.HasVariablePeriod() && args.TryGetValue("period", out var periodParam) &&
                !int.TryParse(periodParam, out period))
            {
                throw new ArgumentException("Period parameter cannot be parsed");
            }

            var counter = 0;
            if (type == AuthenticatorType.Hotp && args.TryGetValue("counter", out var counterParam) &&
                !int.TryParse(counterParam, out counter))
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

            var icon = iconResolver.FindServiceKeyByName(args.TryGetValue("icon", out var iconParam)
                ? iconParam
                : issuer);
            
            var secret = SecretUtil.Clean(args["secret"], type);

            var pinLength = 0;

            if (type == AuthenticatorType.YandexOtp && args.TryGetValue("pin_length", out var pinLengthParam) &&
                !int.TryParse(pinLengthParam, out pinLength))
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
            var match = OtpAuthMigrationRegex().Match(real);

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