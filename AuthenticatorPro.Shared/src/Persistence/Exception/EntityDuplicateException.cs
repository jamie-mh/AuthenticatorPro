// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Shared.Persistence.Exception
{
    public class EntityDuplicateException : System.Exception
    {
        public EntityDuplicateException(System.Exception inner) : base(inner.Message, inner)
        {
        }
    }
}