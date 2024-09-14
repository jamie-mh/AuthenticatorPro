// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Core.Backup
{
    public class RestoreResult : IResult
    {
        public int AddedAuthenticatorCount { get; set; }
        public int UpdatedAuthenticatorCount { get; set; }
        public int AddedCategoryCount { get; set; }
        public int UpdatedCategoryCount { get; set; }
        public int AddedAuthenticatorCategoryCount { get; set; }
        public int UpdatedAuthenticatorCategoryCount { get; set; }
        public int AddedCustomIconCount { get; set; }

        public bool IsVoid()
        {
            return AddedAuthenticatorCount == 0 && UpdatedAuthenticatorCount == 0 && AddedCategoryCount == 0 &&
                   UpdatedCategoryCount == 0 && AddedAuthenticatorCategoryCount == 0 &&
                   UpdatedAuthenticatorCategoryCount == 0 && AddedCustomIconCount == 0;
        }
    }
}