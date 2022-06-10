// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace AuthenticatorPro.Shared.Data.Backup
{
    public class BackupResult : IResult
    {
        public readonly string FileName;

        public BackupResult(string fileName = null)
        {
            FileName = fileName;
        }

        public bool IsVoid()
        {
            return String.IsNullOrEmpty(FileName);
        }
    }
}