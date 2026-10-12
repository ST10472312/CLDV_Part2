using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebStorage"];
            var containerName = configuration["StaffDocsContainerName"];

            if(string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "AzureWebStorage connection string is not configured.");

            if(string.IsNullOrWhiteSpace(containerName))
                throw new InvalidOperationException(
                    "StaffDocsContainerName is not configured.");

            _containerClient =
                new BlobContainerClient(connectionString, containerName);

            _containerClient.CreateIfNotExists(
                PublicAccessType.None);
        }

        public async Task UploadDocumentAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            BlobHttpHeaders headers = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = headers
                });
        }
        public async Task<List<object>> ListDocumentsAsync()
        {
            List<object> documents = new List<object>();

            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                documents.Add(new
                {
                    FileName = blobItem.Name,
                    Size = blobItem.Properties.ContentLength,
                    ContentType = blobItem.Properties.ContentType,
                    LastModified = blobItem.Properties.LastModified
                });
            }

            return documents;
        }
        public async Task<(Stream FileStream, string ContentType)?> DownloadDocumentAsync(
            string fileName)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var download =
                await blobClient.DownloadStreamingAsync();

            string contentType =
                download.Value.Details.ContentType
                ?? "application/octet-stream";

            return (
                download.Value.Content,
                contentType
            );
        }

        public async Task<DocumentMetadata> UploadAsync(string fileName, Stream content, long length, string contentType)
        {
            // reuse existing upload implementation
            await UploadDocumentAsync(content, fileName, contentType);

            // Create and return metadata using the positional constructor
            return new DocumentMetadata(
                fileName,
                length,
                DateTimeOffset.UtcNow,
                contentType
            );
        }

        public async Task<IReadOnlyList<DocumentMetadata>> ListAsync()
        {
            var documents = new List<DocumentMetadata>();

            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                documents.Add(new DocumentMetadata(
                    blobItem.Name,
                    blobItem.Properties.ContentLength ?? 0,
                    blobItem.Properties.LastModified ?? DateTimeOffset.MinValue,
                    blobItem.Properties.ContentType
                ));
            }

            return documents;
        }

        public async Task<(Stream content, string ContentType)?> DownloadAsync(string fileName)
        {
            var result = await DownloadDocumentAsync(fileName);
            if (result == null) return null;
            return (result.Value.FileStream, result.Value.ContentType);
        }
    }

    public record DocumentMetadata : IEquatable<DocumentMetadata>
    {
        public string FileName { get; init; }
        public long Size { get; init; }

        // keep the old name as an alias if other code expects it
        public long SizeBytes => Size;

        public DateTimeOffset LastModified { get; init; }
        public string ContentType { get; init; }

        // simple read-only display; adjust formatting as needed
        public string SizeDisplay => $"{Size} bytes";

        public DocumentMetadata(string fileName, long size, DateTimeOffset lastModified, string contentType)
        {
            FileName = fileName;
            Size = size;
            LastModified = lastModified;
            ContentType = contentType;
        }
    }
    }