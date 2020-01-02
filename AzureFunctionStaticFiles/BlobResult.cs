using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// Storage blob result.
    /// </summary>
    public class BlobResult : FileStreamResult
    {
        /// <summary>
        /// Format a byte array as a hexadecimal string.
        /// </summary>
        private static string FormatMd5Bytes(byte[] raw)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                sb.Append(raw[i].ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public BlobResult(BlobDownloadInfo blob)
            : base(blob.Content, blob.ContentType)
        {
            var md5 = FormatMd5Bytes(blob.Details.BlobContentHash);

            EntityTag = new EntityTagHeaderValue($"\"{md5}\"");
        }
    }
}
