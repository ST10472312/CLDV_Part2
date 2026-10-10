using System.Net;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class DeleteMenuItem
    {
        private readonly TableStorageService _tableStorageService;

        public DeleteMenuItem(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
                Route = "menu/{category}/{id}")]
            HttpRequestData req,
            string category,
            string id)
        {
            try
            {
                var existingItem =
                    await _tableStorageService.GetMenuItemAsync(category, id);

                if (existingItem == null)
                {
                    var notFound =
                        req.CreateResponse(HttpStatusCode.NotFound);

                    await notFound.WriteStringAsync(
                        "Menu item not found.");

                    return notFound;
                }

                await _tableStorageService
                    .DeleteMenuItemAsync(category, id);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteStringAsync(
                    "Menu item deleted successfully.");

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