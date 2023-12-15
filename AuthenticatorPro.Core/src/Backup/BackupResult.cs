// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Core.Backup
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
            return string.IsNullOrEmpty(FileName);
        }
    }
}