// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared;
using Newtonsoft.Json;

namespace AuthenticatorPro.Droid.Shared.Wear
{
    public class WearCustomIcon
    {
        public readonly string Id;

        [JsonConverter(typeof(ByteArrayConverter))]
        public readonly byte[] Data;

        public WearCustomIcon(string id, byte[] data)
        {
            Id = id;
            Data = data;
        }
    }
}