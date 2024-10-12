// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Stratum.Core.Entity;

namespace Stratum.Core.Backup
{
    public class Backup
    {
        public const string FileExtension = "stratum";
        public const string MimeType = "application/octet-stream";

        public IEnumerable<Authenticator> Authenticators { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<AuthenticatorCategory> AuthenticatorCategories { get; set; }
        public IEnumerable<CustomIcon> CustomIcons { get; set; }
    }
}