using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs.Models;

namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// Get function definition.
    /// </summary>
    public class Get
    {
        /// <summary>
        /// Normalise the path of an object within a container.
        /// </summary>
        /// <param name="path">
        /// The path, relative to the root of the container.
        /// </param>
        /// <param name="requestPath">
        /// The complete request path.
        /// </param>
        /// <returns>
        /// A non-null path within the container. If no path was specified, an empty string.
        /// </returns>
        private static string NormalisePath(string path, string requestPath)
        {
            if (string.IsNullOrEmpty(path) && requestPath.EndsWith("/"))
            {
                return "/";
            }
            else if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            else if (!path.StartsWith("/"))
            {
                return $"/{path}";
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// Obtain a blob from a container.
        /// </summary>
        private static async Task<BlobDownloadInfo> GetBlob(BlobContainerClient container, string name)
        {
            var blob = container.GetBlobClient(name);

            return await blob.DownloadAsync();
        }

        /// <summary>
        /// Serve a blob.
        /// </summary>
        /// <param name="baseUri">
        /// Base URI for redirects.
        /// </param>
        /// <param name="log">
        /// Logger instance to record response statuses.
        /// </param>
        /// <param name="container">
        /// A client for the parent container.
        /// </param>
        /// <param name="path">
        /// Path of the blob within the container.
        /// </param>
        /// <param name="indexName">
        /// Container index filename.
        /// </param>
        private static async Task<ActionResult> ServeBlob(
            string baseUri, ILogger log, BlobContainerClient container, string path, string indexName)
        {
            // Requests to the root must include the preceeding / to preserve relative links.
            if (string.IsNullOrEmpty(path)) {
                log.LogInformation($"GET {path} 301 ({path}/)");
                return new RedirectResult($"{baseUri}{path}/", true);
            }

            // Requests for directories should always yield the index.
            if (!string.IsNullOrEmpty(indexName)
                && (string.IsNullOrEmpty(path) || path.EndsWith("/")))
            {
                path += indexName;
            }

            var name = path.Substring(1);
            try {
                var blob = await GetBlob(container, name);
                log.LogInformation($"GET {path} 200 ({blob.ContentType}; {blob.Details.BlobContentHash})");
                return new BlobResult(blob);
            }
            catch (Azure.RequestFailedException exception)
            {
                if (exception.ErrorCode == "BlobNotFound")
                {
                    if (!string.IsNullOrEmpty(indexName)
                        && Path.GetFileName(path) != indexName)
                    {
                        // Maybe we hit a nested container -- check to see if there's an index in there.
                        try
                        {
                            var indexBlob = container.GetBlobClient($"{name}/{indexName}");
                            indexBlob.GetProperties();
                            // It exists, so redirect to the parent directory; redirects ensure we don't break
                            // relatively linked resources.
                            return new RedirectResult($"{baseUri}{path}/", true);
                        }
                        catch (Azure.RequestFailedException indexException)
                        {
                            if (indexException.ErrorCode != "BlobNotFound")
                            {
                                throw indexException;
                            }
                        }
                    }
                    log.LogWarning($"GET {path} 404");
                    return new HttpStatusMessageResult(StatusCodes.Status404NotFound);
                }
                else
                {
                    log.LogError($"GET {path} 500 (Azure.RequestFailedException {exception.ErrorCode} {exception.Message})");
                    return new HttpStatusMessageResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception exception)
            {
                log.LogError($"GET {path} 500 ({exception.GetType()} {exception.Message})");
                return new HttpStatusMessageResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Frontend configuration options.
        /// </summary>
        private FrontendOptions FrontendOptions;

        /// <summary>
        /// Storage configuration options.
        /// </summary>
        private StorageOptions StorageOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frontendOptions">
        /// Frontend configuration options.
        /// </param>
        /// <param name="storageOptions">
        /// Storage configuration options.
        /// </param>
        public Get(
            IOptions<FrontendOptions> frontendOptions,
            IOptions<StorageOptions> storageOptions)
        {
            FrontendOptions = frontendOptions.Value;
            StorageOptions = storageOptions.Value;
        }

        /// <summary>
        /// Handle a request for a blob.
        /// </summary>
        /// <param name="req">
        /// HTTP request.
        /// </param>
        /// <param name="containerName">
        /// Name of the blob container.
        /// </param>
        /// <param name="path">
        /// Path of the blob (including preceding forward slash ("/")).
        /// </param>
        /// <param name="log">
        /// Logger instance to record response statuses.
        /// </param>
        /// <param name="context">
        /// WebJobs execution context.
        /// </param>
        [FunctionName("Get")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{containerName}/{*path}")] HttpRequest req,
            string containerName, string path,
            ILogger log, ExecutionContext context)
        {
            path = NormalisePath(path, req.Path.Value);

            string basePath = req.Path.Value;
            if (!string.IsNullOrEmpty(path))
            {
                basePath = req.Path.Value.Substring(0, basePath.LastIndexOf(path));
            }
            string host = FrontendOptions.HostName.ValueOrDefault(req.Host.Value);
            string baseUri = $"{req.Scheme}://{host}{basePath}";

            var blobService = new BlobServiceClient(StorageOptions.AccountConnectionString);
            var container = blobService.GetBlobContainerClient(containerName);

            return await ServeBlob(baseUri, log, container, path, StorageOptions.IndexName);
        }
    }
}
