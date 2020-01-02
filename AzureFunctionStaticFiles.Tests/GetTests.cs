using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AzureFunctionStaticFiles.Tests
{
    /// <summary>
    /// Tests for the Get method.
    /// </summary>
    public class GetTests
    {
        /// <summary>
        /// Online connection string.
        /// </summary>
        private const string DevelopmentConnectionString = @"UseDevelopmentStorage=true;";

        /// <summary>
        /// Offline connection string.
        /// </summary>
        private const string OfflineConnectionString = @"UseDevelopmentStorage=false;";

        /// <summary>
        /// Container index object name.
        /// </summary>
        private const string IndexName = @"index.html";

        /// <summary>
        /// Create a logger instance.
        /// </summary>
        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;

            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            } else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null logger");
            }

            return logger;
        }

        /// <summary>
        /// Create an HTTP request.
        /// </summary>
        /// <param name="path">
        /// Request path.
        /// </param>
        public static DefaultHttpRequest CreateHttpRequest(string path)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = path,
            };
        }

        /// <summary>
        /// Create a Function App execution context.
        /// </summary>
        public static ExecutionContext CreateExecutionContext(string functionName)
        {
            return new ExecutionContext()
            {
                FunctionAppDirectory = Environment.CurrentDirectory,
                FunctionName = functionName,
            };
        }

        /// <summary>
        /// Create a blob container that'll get deleted when it goes out of scope.
        /// </summary>
        /// <param name="containerName">
        /// The name of the container to create.
        /// </param>
        /// <param name="connectionString">
        /// Optional connection string; defaults to using development storage on the emulator.
        /// </param>
        private Deletable<BlobContainerClient> CreateBlobContainer(string containerName, string connectionString = null)
        {
            connectionString = string.IsNullOrEmpty(connectionString) ? DevelopmentConnectionString : connectionString;
            var blobService = new BlobServiceClient(connectionString);
            return new Deletable<BlobContainerClient>(blobService.CreateBlobContainer(containerName).Value);
        }

        /// <summary>
        /// Create a blob and upload the file at the specified path.
        /// </summary>
        /// <param name="container">
        /// Parent container.
        /// </param>
        /// <param name="name">
        /// Name of the blob.
        /// </param>
        /// <param name="path">
        /// Path to the local file to upload.
        /// </param>
        /// <param name="contentType">
        /// The type of the content.
        /// </param>
        private BlobContentInfo CreateStorageBlob(
            BlobContainerClient container, string name, string path, string contentType)
        {
            using FileStream content = File.OpenRead(Path.Combine("fixtures", path));

            var blob = container.GetBlockBlobClient(name);
            var headers = new BlobHttpHeaders()
            {
                ContentType = contentType,
            };
            return blob.Upload(content, headers).Value;
        }

        /// <summary>
        /// Create frontend options.
        /// </summary>
        /// <param name="hostName">
        /// Host name of the site.
        /// </param>
        public FrontendOptions CreateFrontendOptions(string hostName = null)
        {
            return new FrontendOptions
            {
                HostName = hostName,
            };
        }

        /// <summary>
        /// Create storage options.
        /// </summary>
        /// <param name="connectionString">
        /// Storage account connection string.
        /// </param>
        /// <param name="indexName">
        /// Container index filename.
        /// </param>
        public StorageOptions CreateStorageOptions(string connectionString = null, string indexName = null)
        {
            return new StorageOptions
            {
                AccountConnectionString = string.IsNullOrEmpty(connectionString)
                    ? DevelopmentConnectionString : connectionString,
                IndexName = string.IsNullOrEmpty(indexName)
                    ? "index.html" : indexName,
            };
        }

        /// <summary>
        /// Run the function with the specified options.
        /// </summary>
        /// <param name="frontendOptions">
        /// Frontend configuration options.
        /// </param>
        /// <param name="storageOptions">
        /// Storage configuration options.
        /// </param>
        /// <param name="containerName">
        /// Name of the blob container.
        /// </param>
        /// <param name="path">
        /// Path of the blob (including preceding forward slash ("/")).
        /// </param>
        private async Task<IActionResult> RunAsync(
            FrontendOptions frontendOptions, StorageOptions storageOptions,
            string containerName, string path)
        {
            var wrappedFrontendOptions = Options.Create<FrontendOptions>(frontendOptions);
            var wrappedStorageOptions = Options.Create<StorageOptions>(storageOptions);
            var function = new Get(wrappedFrontendOptions, wrappedStorageOptions);

            var req = CreateHttpRequest($"/api/GetStaticFile/{containerName}{path}");
            var log = CreateLogger(LoggerTypes.Null);
            var context = CreateExecutionContext("GetStaticFile");
            return await function.Run(req, containerName, path, log, context);
        }

        public static IEnumerable<object[]> ExistingFiles =>
            new List<object[]>
            {
                new object[] { "index.html", "text/html" },
                new object[] { "image.png", "image/png" },
            };

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async void TestWithExistingFile(string name, string contentType)
        {
            using var container = CreateBlobContainer("testwithexistingfile");
            CreateStorageBlob(container.Value, name, name, contentType);

            var result = await RunAsync(
                CreateFrontendOptions(), CreateStorageOptions(),
                container.Value.Name, $"/{name}");

            Assert.IsType<BlobResult>(result);
            Assert.StartsWith(contentType, ((BlobResult)result).ContentType);
        }

        public static IEnumerable<object[]> MissingFiles =>
            new List<object[]>
            {
                new object[] { "missing.html" },
                new object[] { "missing/missing.html" },
            };

        [Theory]
        [MemberData(nameof(MissingFiles))]
        public async Task TestWithMissingFile(string path)
        {
            using var container = CreateBlobContainer("testwithmissingfile");

            var result = await RunAsync(
                CreateFrontendOptions(), CreateStorageOptions(),
                container.Value.Name, $"/{path}");

            Assert.IsType<HttpStatusMessageResult>(result);
            Assert.Equal(404, ((HttpStatusMessageResult)result).StatusCode);
        }

        public static IEnumerable<object[]> ContainerRedirectPaths =>
            new List<object[]>
            {
                new object[] { null, "/" },
                new object[] { "", "/" },
                new object[] { "/container", "/container/" },
            };

        [Theory]
        [MemberData(nameof(ContainerRedirectPaths))]
        public async Task TestContainerRedirectsToContainerSlash(
            string requestPath, string targetPath)
        {
            using var container = CreateBlobContainer("testcontainerredirectstocontainerslash");
            CreateStorageBlob(container.Value, "index.html", "index.html", "text/html");
            CreateStorageBlob(container.Value, "container/index.html", "index.html", "text/html");

            var result = await RunAsync(
                CreateFrontendOptions(), CreateStorageOptions(),
                container.Value.Name, requestPath);

            Assert.IsType<RedirectResult>(result);
            Assert.EndsWith(targetPath, ((RedirectResult)result).Url);
        }

        public static IEnumerable<object[]> ContainerIndexes =>
            new List<object[]> {
                new object[] { "/" },
                new object[] { "/container/" },
            };

        [Theory]
        [MemberData(nameof(ContainerIndexes))]
        public async Task TestContainerServesIndex(string requestPath)
        {
            using var container = CreateBlobContainer("containerservesindex");
            CreateStorageBlob(container.Value, "index.html", "index.html", "text/html");
            CreateStorageBlob(container.Value, "container/index.html", "index.html", "text/html");

            var result = await RunAsync(
                CreateFrontendOptions(), CreateStorageOptions(),
                container.Value.Name, requestPath);

            Assert.IsType<BlobResult>(result);
            Assert.StartsWith("text/html", ((BlobResult)result).ContentType);
        }

        [Fact]
        public async Task TestHandlingOfStorageOutage()
        {
            var result = await RunAsync(
                    CreateFrontendOptions(), CreateStorageOptions(),
                    "handlingofstorageoutage", "/missing");

            Assert.IsType<HttpStatusMessageResult>(result);
            Assert.Equal(500, ((HttpStatusMessageResult)result).StatusCode);
        }

        public async Task TestRedirectsHonourHostNameOption()
        {
            using var container = CreateBlobContainer("redirectshonourhostnameoption");
            CreateStorageBlob(container.Value, "container/index.html", "index.html", "text/html");

            var frontendOptions = CreateFrontendOptions("hostname.com");
            var result = await RunAsync(
                    frontendOptions, CreateStorageOptions(),
                    container.Value.Name, "/container/index.html");

            Assert.IsType<RedirectResult>(result);
            Assert.Contains($"://{frontendOptions.HostName}/", ((RedirectResult)result).Url);
        }
    }
}
