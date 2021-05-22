// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace AuthenticatorPro.Shared.Util
{
    public static class CodeUtil
    {
        private const int MinCodeGroupSize = 3;
        private const int MaxCodeGroupSize = 4;
        
        public static string PadCode(string code, int digits)
        {
            code ??= new string('-', digits);

            var spacesInserted = 0;
            var groupSize = Math.Min(MaxCodeGroupSize, Math.Max(digits / 2, MinCodeGroupSize));

            for(var i = 0; i < digits; ++i)
            {
                if(i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            return code;
        }
    }
}