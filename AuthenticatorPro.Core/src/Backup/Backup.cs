// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Core.Backup
{
    public class Backup
    {
        public const string FileExtension = "authpro";
        public const string MimeType = "application/octet-stream";

        public IEnumerable<Authenticator> Authenticators { get; }
        public IEnumerable<Category> Categories { get; }
        public IEnumerable<AuthenticatorCategory> AuthenticatorCategories { get; }
        public IEnumerable<CustomIcon> CustomIcons { get; }

        public Backup(IEnumerable<Authenticator> authenticators, IEnumerable<Category> categories = null,
            IEnumerable<AuthenticatorCategory> authenticatorCategories = null,
            IEnumerable<CustomIcon> customIcons = null)
        {
            Authenticators = authenticators ??
                             throw new ArgumentNullException(nameof(authenticators),
                                 "Backup must contain authenticators");
            Categories = categories;
            AuthenticatorCategories = authenticatorCategories;
            CustomIcons = customIcons;
        }
    }
}