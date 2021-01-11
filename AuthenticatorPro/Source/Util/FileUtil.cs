using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Java.IO;
using IOException = System.IO.IOException;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Util
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
            
            if(data == null)
                throw new IOException("File data is null");

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
    }
}