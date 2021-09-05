// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Database;
using Android.Provider;
using Java.IO;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOException = System.IO.IOException;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Util
{
    internal static class FileUtil
    {
        public static async Task<byte[]> ReadFile(Context context, Uri uri)
        {
            MemoryStream memoryStream = null;
            Stream stream = null;
            byte[] data = null;

            try
            {
                await Task.Run(async delegate
                {
                    stream = context.ContentResolver.OpenInputStream(uri);
                    memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    data = memoryStream.ToArray();
                });
            }
            finally
            {
                memoryStream?.Close();
                stream?.Close();
            }

            if (data == null)
            {
                throw new IOException("File data is null");
            }

            return data;
        }

        public static async Task WriteFile(Context context, Uri uri, byte[] data)
        {
            // Run backup on separate thread, file writing on the main thread fail when using Nextcloud
            await Task.Run(async delegate
            {
                // This is the only way of reliably writing files using SAF on Xamarin.
                // A file output stream will usually create 0 byte files on virtual storage such as Google Drive
                var output = context.ContentResolver.OpenOutputStream(uri, "rwt");
                var dataStream = new DataOutputStream(output);

                try
                {
                    await dataStream.WriteAsync(data);
                    await dataStream.FlushAsync();
                }
                finally
                {
                    dataStream.Close();
                    output.Close();
                }
            });
        }

        public static async Task WriteFile(Context context, Uri uri, string data)
        {
            Stream output = null;
            BufferedWriter writer = null;

            try
            {
                await Task.Run(async delegate
                {
                    output = context.ContentResolver.OpenOutputStream(uri);
                    writer = new BufferedWriter(new OutputStreamWriter(output));

                    await writer.WriteAsync(data);
                    await writer.FlushAsync();
                });
            }
            finally
            {
                writer?.Close();
                output?.Close();
            }
        }

        public static string GetDocumentName(ContentResolver resolver, Uri uri)
        {
            string name = null;

            if (uri.Scheme == "content")
            {
                ICursor cursor = null;

                try
                {
                    var documentUri =
                        DocumentsContract.BuildDocumentUriUsingTree(uri, DocumentsContract.GetTreeDocumentId(uri));

                    if (documentUri == null)
                    {
                        throw new Exception("Cannot get document URI");
                    }

                    cursor = resolver.Query(documentUri, null, null, null, null);

                    if (cursor != null && cursor.MoveToFirst())
                    {
                        var index = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
                        name = cursor.GetString(index);
                    }
                }
                finally
                {
                    cursor?.Close();
                }
            }
            else
            {
                name = uri.LastPathSegment?.Split(':', 2).Last();
            }

            return name;
        }
    }
}