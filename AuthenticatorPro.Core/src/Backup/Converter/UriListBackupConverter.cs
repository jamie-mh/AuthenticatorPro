// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Backup.Converter
{
    public class UriListBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public UriListBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var text = Encoding.UTF8.GetString(data);
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var line in lines)
            {
                Authenticator auth;

                try
                {
                    auth = Authenticator.ParseUri(line, IconResolver).Authenticator;
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

            var backup = new Backup(authenticators);
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }
    }
}