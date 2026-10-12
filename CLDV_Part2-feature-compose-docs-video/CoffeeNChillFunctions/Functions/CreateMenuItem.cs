using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class CreateMenuItem
    {
        private readonly TableStorageService _tableStorageService;

        public CreateMenuItem(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                MenuItem? item = JsonSerializer.Deserialize<MenuItem>(
                    requestBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (item == null)
                {
                    HttpResponseData badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Invalid menu item data.");

                    return badResponse;
                }

                if (string.IsNullOrWhiteSpace(item.PartitionKey) ||
                    string.IsNullOrWhiteSpace(item.RowKey) ||
                    string.IsNullOrWhiteSpace(item.Name))
                {
                    HttpResponseData badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Category, ID and Name are required.");

                    return badResponse;
                }

                if (item.Price < 0)
                {
                    HttpResponseData badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Price cannot be negative.");

                    return badResponse;
                }

                await _tableStorageService.AddMenuItemAsync(item);

                HttpResponseData response =
                    req.CreateResponse(HttpStatusCode.Created);

                await response.WriteAsJsonAsync(item);

                return response;
            }
            catch (Exception ex)
            {
                HttpResponseData errorResponse =
                    req.CreateResponse(HttpStatusCode.InternalServerError);

                await errorResponse.WriteStringAsync(
                    $"An error occurred: {ex.Message}");

                return errorResponse;
            }
        }
    }
}