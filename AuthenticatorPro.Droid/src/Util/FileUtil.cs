// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using Java.IO;
using IOException = System.IO.IOException;

namespace AuthenticatorPro.Droid.Util
{
    internal static class FileUtil
    {
        public static Task<byte[]> ReadFile(Context context, Uri uri)
        {
            return Task.Run(async delegate
            {
                MemoryStream memoryStream = null;
                Stream stream = null;
                byte[] data;

                try
                {
                    stream = context.ContentResolver.OpenInputStream(uri);
                    memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    data = memoryStream.ToArray();
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
            });
        }

        public static Task WriteFile(Context context, Uri uri, byte[] data)
        {
            // Run backup on separate thread, file writing on the main thread fails when using Nextcloud
            return Task.Run(async delegate
            {
                // This is the only way of reliably writing binary files using SAF on Xamarin.
                // A file output stream will usually create 0 byte files on virtual storage such as Google Drive
                Stream output = null;
                DataOutputStream dataStream = null;

                try
                {
                    output = context.ContentResolver.OpenOutputStream(uri);
                    dataStream = new DataOutputStream(output);

                    await dataStream.WriteAsync(data);
                    await dataStream.FlushAsync();
                }
                finally
                {
                    dataStream?.Close();
                    output?.Close();
                }
            });
        }

        public static Task WriteFile(Context context, Uri uri, string data)
        {
            return Task.Run(async delegate
            {
                Stream output = null;
                OutputStreamWriter outputWriter = null;
                BufferedWriter bufferedWriter = null;

                try
                {
                    output = context.ContentResolver.OpenOutputStream(uri);
                    outputWriter = new OutputStreamWriter(output);
                    bufferedWriter = new BufferedWriter(outputWriter);

                    await bufferedWriter.WriteAsync(data);
                    await bufferedWriter.FlushAsync();
                }
                finally
                {
                    bufferedWriter?.Close();
                    outputWriter?.Close();
                    output?.Close();
                }
            });
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
                        throw new IOException("Cannot get document URI");
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