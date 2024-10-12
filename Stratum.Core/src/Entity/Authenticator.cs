// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
using SQLite;
using Stratum.Core.Generator;
using Stratum.Core.Util;

namespace Stratum.Core.Entity
{
    [Table("authenticator")]
    public class Authenticator
    {
        public const int IssuerMaxLength = 32;
        public const int UsernameMaxLength = 40;

        public const HashAlgorithm DefaultAlgorithm = HashAlgorithm.Sha1;

        private IGenerator _generator;
        private long _lastCounter;
        private string _code;

        public Authenticator()
        {
            _code = null;
            _generator = null;
            _lastCounter = 0;

            Algorithm = DefaultAlgorithm;
            Type = AuthenticatorType.Totp;
            Digits = Type.GetDefaultDigits();
            Period = Type.GetDefaultPeriod();
        }

        [Column("type")]
        public AuthenticatorType Type { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("issuer")]
        [MaxLength(IssuerMaxLength)]
        public string Issuer { get; set; }

        [Column("username")]
        [MaxLength(UsernameMaxLength)]
        public string Username { get; set; }

        [Column("secret")]
        [PrimaryKey]
        public string Secret { get; set; }

        [Column("pin")]
        public string Pin { get; set; }

        [Column("algorithm")]
        public HashAlgorithm Algorithm { get; set; }

        [Column("digits")]
        public int Digits { get; set; }

        [Column("period")]
        public int Period { get; set; }

        [Column("counter")]
        public long Counter { get; set; }

        [Column("copyCount")]
        public int CopyCount { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }

        public string GetCode(long counter)
        {
            _generator ??= Type switch
            {
                AuthenticatorType.Totp => new Totp(Secret, Period, Algorithm, Digits),
                AuthenticatorType.Hotp => new Hotp(Secret, Algorithm, Digits),
                AuthenticatorType.MobileOtp => new MobileOtp(Secret, Pin),
                AuthenticatorType.SteamOtp => new SteamOtp(Secret),
                AuthenticatorType.YandexOtp => new YandexOtp(Secret, Pin),
                _ => throw new ArgumentException("Unknown authenticator type")
            };

            switch (Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    _code = _generator.Compute(counter);
                    break;

                case GenerationMethod.Counter when _lastCounter == Counter:
                    return _code;

                case GenerationMethod.Counter:
                {
                    _code = _generator.Compute(Counter);
                    _lastCounter = Counter;
                    break;
                }
            }

            return _code;
        }

        public string GetCode()
        {
            long counter;

            switch (Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    counter = now - now % Period;
                    break;
                }

                case GenerationMethod.Counter:
                    counter = Counter;
                    break;

                default:
                    throw new ArgumentException("Unknown generation method");
            }

            return GetCode(counter);
        }

        private string GetMotpUri()
        {
            var builder = new StringBuilder("motp://");
            builder.Append(Issuer);
            builder.Append(':');
            builder.Append(Username);
            builder.Append("?secret=");
            builder.Append(Secret);

            return builder.ToString();
        }

        private string GetOtpAuthUri()
        {
            var type = Type switch
            {
                AuthenticatorType.Hotp => "hotp",
                AuthenticatorType.Totp => "totp",
                AuthenticatorType.SteamOtp => "totp",
                AuthenticatorType.YandexOtp => "yaotp",
                _ => throw new NotSupportedException("Unsupported authenticator type")
            };

            var issuerUsername = string.IsNullOrEmpty(Username) ? Issuer : $"{Issuer}:{Username}";

            var uri = new StringBuilder(
                $"otpauth://{type}/{Uri.EscapeDataString(issuerUsername)}?secret={Secret}&issuer={Uri.EscapeDataString(Issuer)}");

            if (Algorithm != DefaultAlgorithm)
            {
                var algorithmName = Algorithm switch
                {
                    HashAlgorithm.Sha256 => "SHA256",
                    HashAlgorithm.Sha512 => "SHA512",
                    _ => throw new NotSupportedException("Unsupported algorithm")
                };

                uri.Append($"&algorithm={algorithmName}");
            }

            if (Digits != Type.GetDefaultDigits())
            {
                uri.Append($"&digits={Digits}");
            }

            switch (Type)
            {
                case AuthenticatorType.Totp when Period != Type.GetDefaultPeriod():
                    uri.Append($"&period={Period}");
                    break;

                case AuthenticatorType.Hotp:
                    uri.Append($"&counter={Counter}");
                    break;

                case AuthenticatorType.SteamOtp when Issuer != "Steam":
                    uri.Append("&steam");
                    break;

                case AuthenticatorType.YandexOtp when Pin != null:
                    uri.Append($"&pin_length={Pin.Length}");
                    break;
            }

            return uri.ToString();
        }

        public virtual string GetUri()
        {
            return Type == AuthenticatorType.MobileOtp ? GetMotpUri() : GetOtpAuthUri();
        }

        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Issuer))
            {
                throw new ArgumentException("Issuer cannot be null or empty");
            }

            if (Digits < Type.GetMinDigits())
            {
                throw new ArgumentException($"Too few digits, expected at least {Type.GetMinDigits()}");
            }

            if (Digits > Type.GetMaxDigits())
            {
                throw new ArgumentException($"Too many digits, expected fewer than {Type.GetMaxDigits()}");
            }

            if (Type.GetGenerationMethod() == GenerationMethod.Time && Period <= 0)
            {
                throw new ArgumentException("Period cannot be negative or zero");
            }

            SecretUtil.Validate(Secret, Type);
        }
    }
}