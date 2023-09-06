// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Android.Content;
using Android.Net;

namespace AuthenticatorPro.Droid.QrCode.Reader
{
    public interface IQrCodeReader
    {
        Task<string> ScanImageFromFileAsync(Context context, Uri uri);
    }
}