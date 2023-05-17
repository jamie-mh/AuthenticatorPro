// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core;
using Newtonsoft.Json;

namespace AuthenticatorPro.Droid.Shared.Wear
{
    public class WearCustomIcon
    {
        public string Id { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }
    }
}