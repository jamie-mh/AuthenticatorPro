// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using SimpleBase;

namespace AuthenticatorPro.Core.Converter
{
    public class GoogleAuthenticatorBackupConverter : BackupConverter
    {
        public GoogleAuthenticatorBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var uri = Encoding.UTF8.GetString(data);
            var migration = UriParser.ParseOtpAuthMigrationUri(uri);

            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var item in migration.Authenticators)
            {
                Authenticator auth;

                try
                {
                    auth = ConvertMigrationAuthenticator(item);
                }
                catch (ArgumentException e)
                {
                    failures.Add(new ConversionFailure { Description = item.Issuer, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);
            }

            var result = new ConversionResult
            {
                Failures = failures, Backup = new Backup.Backup { Authenticators = authenticators }
            };

            return Task.FromResult(result);
        }

        private Authenticator ConvertMigrationAuthenticator(OtpAuthMigration.MigrationAuthenticator auth)
        {
            string issuer;
            string username;

            // Google Auth may not have an issuer, just use the username instead
            if (string.IsNullOrEmpty(auth.Issuer))
            {
                issuer = auth.Username.Trim().Truncate(Authenticator.IssuerMaxLength);
                username = null;
            }
            else
            {
                issuer = auth.Issuer.Trim().Truncate(Authenticator.IssuerMaxLength);
                // For some odd reason the username field always follows a '[issuer]: [username]' format
                username = auth.Username
                    .Replace($"{auth.Issuer}: ", "")
                    .Trim()
                    .Truncate(Authenticator.UsernameMaxLength);
            }

            var type = auth.Type switch
            {
                OtpAuthMigration.Type.Totp => AuthenticatorType.Totp,
                OtpAuthMigration.Type.Hotp => AuthenticatorType.Hotp,
                _ => throw new ArgumentException($"Unknown type '{auth.Type}")
            };

            var algorithm = auth.Algorithm switch
            {
                OtpAuthMigration.Algorithm.Sha1 => HashAlgorithm.Sha1,
                OtpAuthMigration.Algorithm.Sha256 => HashAlgorithm.Sha256,
                OtpAuthMigration.Algorithm.Sha512 => HashAlgorithm.Sha512,
                _ => throw new ArgumentException($"Unknown algorithm '{auth.Algorithm}")
            };

            string secret;

            try
            {
                secret = Base32.Rfc4648.Encode(auth.Secret);
                secret = SecretUtil.Clean(secret, type);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to parse secret", e);
            }

            var result = new Authenticator
            {
                Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                Username = username.Truncate(Authenticator.UsernameMaxLength),
                Algorithm = algorithm,
                Type = type,
                Secret = secret,
                Counter = auth.Counter,
                Digits = type.GetDefaultDigits(),
                Period = type.GetDefaultPeriod(),
                Icon = IconResolver.FindServiceKeyByName(issuer)
            };

            result.Validate();
            return result;
        }
    }
}