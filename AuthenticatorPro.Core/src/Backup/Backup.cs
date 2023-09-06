// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Backup
{
    public class Backup
    {
        public const string FileExtension = "authpro";
        public const string MimeType = "application/octet-stream";

        public IEnumerable<Authenticator> Authenticators { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<AuthenticatorCategory> AuthenticatorCategories { get; set; }
        public IEnumerable<CustomIcon> CustomIcons { get; set; }
    }
}