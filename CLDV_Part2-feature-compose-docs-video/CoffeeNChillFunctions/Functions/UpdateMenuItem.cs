using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class UpdateMenuItem
    {
        private readonly TableStorageService _tableStorageService;

        public UpdateMenuItem(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "menu/{category}/{id}")]
            HttpRequestData req,
            string category,
            string id)
        {
            try
            {
                MenuItem? existingItem =
                    await _tableStorageService.GetMenuItemAsync(category, id);

                if (existingItem == null)
                {
                    var notFound =
                        req.CreateResponse(HttpStatusCode.NotFound);

                    await notFound.WriteStringAsync(
                        "Menu item not found.");

                    return notFound;
                }

                string requestBody =
                    await new StreamReader(req.Body).ReadToEndAsync();

                MenuItem? updatedData =
                    JsonSerializer.Deserialize<MenuItem>(
                        requestBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (updatedData == null)
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteStringAsync(
                        "Invalid menu item data.");

                    return badRequest;
                }

                if (updatedData.Price < 0)
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteStringAsync(
                        "Price cannot be negative.");

                    return badRequest;
                }

                existingItem.Name = updatedData.Name;
                existingItem.Description = updatedData.Description;
                existingItem.Price = updatedData.Price;
                existingItem.IsAvailable = updatedData.IsAvailable;

                await _tableStorageService.UpdateMenuItemAsync(existingItem);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(existingItem);

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