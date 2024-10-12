// Copyright (C) 2024 jmh
// SPDX-License-Identifier:GPL-3.0-only

using System;

namespace Stratum.Core.Backup
{
    public class BackupPasswordException : Exception
    {
        public BackupPasswordException(string message, Exception inner) : base(message, inner)
        {
        }
        
        public BackupPasswordException(string message) : base(message)
        {
        }
    }
}