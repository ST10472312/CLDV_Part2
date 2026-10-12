using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace CoffeeNChillFunctions.Functions;

public class UploadStaffDocument
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public UploadStaffDocument(
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    [Function("UploadStaffDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "documents/upload")]
        HttpRequest req)
    {
        try
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult(
                    "Request must use multipart/form-data.");
            }

            var form = await req.ReadFormAsync();

            var file = form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult(
                    "A document must be supplied.");
            }

            string fileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new BadRequestObjectResult(
                    "A valid file name is required.");
            }

            string extension =
                Path.GetExtension(fileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".pdf",
                ".doc",
                ".docx",
                ".txt"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return new BadRequestObjectResult(
                    "Only PDF, DOC, DOCX and TXT files are allowed.");
            }

            long maxUploadBytes = 10 * 1024 * 1024;

            string? configuredMax =
                _configuration["MaxUploadBytes"];

            if (long.TryParse(
                configuredMax,
                out long configuredValue))
            {
                maxUploadBytes = configuredValue;
            }

            if (file.Length > maxUploadBytes)
            {
                return new BadRequestObjectResult(
                    $"File exceeds the maximum allowed size of " +
                    $"{maxUploadBytes / (1024 * 1024)} MB.");
            }

            string contentType =
                string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType;

            using Stream fileStream = file.OpenReadStream();

            var metadata =
                await _blobStorageService.UploadAsync(
                    fileName,
                    fileStream,
                    file.Length,
                    contentType);

            return new CreatedResult(
                $"/api/documents/download/{Uri.EscapeDataString(fileName)}",
                metadata);
        }
        catch (Exception ex)
        {
            return new ObjectResult(
                $"An error occurred while uploading the document: {ex.Message}")
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}