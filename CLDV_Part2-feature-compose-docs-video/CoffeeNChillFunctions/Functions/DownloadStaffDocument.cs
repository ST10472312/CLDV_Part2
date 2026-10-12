using System.Net;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class DownloadStaffDocument
    {
        private readonly IBlobStorageService _blobStorageService;

        public DownloadStaffDocument(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents/download/{fileName}")]
            HttpRequestData req,
            string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    var badRequest =
                        req.CreateResponse(
                            HttpStatusCode.BadRequest);

                    await badRequest.WriteStringAsync(
                        "File name is required.");

                    return badRequest;
                }

                var result =
                    await _blobStorageService
                        .DownloadAsync(fileName);

                if (result == null)
                {
                    var notFound =
                        req.CreateResponse(
                            HttpStatusCode.NotFound);

                    await notFound.WriteStringAsync(
                        "Document not found.");

                    return notFound;
                }

                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                response.Headers.Add(
                    "Content-Type",
                    result.Value.ContentType);

                response.Headers.Add(
                    "Content-Disposition",
                    $"attachment; filename=\"{fileName}\"");

                await result.Value.content
                    .CopyToAsync(response.Body);

                return response;
            }
            catch (Exception ex)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    $"An error occurred: {ex.Message}");

                return response;
            }
        }
    }
}