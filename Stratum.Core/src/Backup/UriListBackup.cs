// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;
using Stratum.Core.Entity;

namespace Stratum.Core.Backup
{
    public class UriListBackup
    {
        public const string FileExtension = "txt";
        public const string MimeType = "text/plain";

        private readonly string _contents;

        private UriListBackup(string contents)
        {
            _contents = contents;
        }

        public override string ToString()
        {
            return _contents;
        }

        public static UriListBackup FromAuthenticators(IEnumerable<Authenticator> authenticators)
        {
            var builder = new StringBuilder();

            foreach (var auth in authenticators)
            {
                string uri;

                try
                {
                    uri = auth.GetUri();
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                builder.AppendLine(uri);
            }

            return new UriListBackup(builder.ToString());
        }
    }
}