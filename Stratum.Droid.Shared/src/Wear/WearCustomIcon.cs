// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core;
using Newtonsoft.Json;

namespace Stratum.Droid.Shared.Wear
{
    public class WearCustomIcon
    {
        public string Id { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }
    }
}