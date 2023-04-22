// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Converter
{
    public class UriListBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public UriListBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var text = Encoding.UTF8.GetString(data);
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (!lines.Any(l => l.StartsWith("otpauth")))
            {
                throw new ArgumentException("Invalid file");
            }

            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var line in lines)
            {
                Authenticator auth;

                try
                {
                    auth = UriParser.ParseStandardUri(line, IconResolver).Authenticator;
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure
                    {
                        Description = line,
                        Error = e.Message
                    });

                    continue;
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }
    }
}