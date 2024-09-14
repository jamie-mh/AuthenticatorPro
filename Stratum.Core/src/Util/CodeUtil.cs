// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Core.Util
{
    public static class CodeUtil
    {
        public static string PadCode(string code, int digits, int groupSize)
        {
            code ??= new string('–', digits);
            var spacesInserted = 0;

            if (groupSize <= 0)
            {
                groupSize = digits;
            }

            for (var i = 0; i < digits; ++i)
            {
                if (i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            return code;
        }
    }
}