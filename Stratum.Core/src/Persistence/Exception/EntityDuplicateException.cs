// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Core.Persistence.Exception
{
    public class EntityDuplicateException : System.Exception
    {
        public EntityDuplicateException(System.Exception inner) : base(inner.Message, inner)
        {
        }

        public EntityDuplicateException()
        {
        }
    }
}