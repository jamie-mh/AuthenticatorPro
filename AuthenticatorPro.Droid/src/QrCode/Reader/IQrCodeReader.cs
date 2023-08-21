// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using System.Threading.Tasks;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.QrCode.Reader
{
    public interface IQrCodeReader
    {
        Task<string> ScanImageFromFileAsync(Context context, Uri uri);
    }
}