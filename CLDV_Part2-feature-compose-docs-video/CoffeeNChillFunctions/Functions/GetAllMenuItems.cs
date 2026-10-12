using System.Net;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class GetAllMenuItems
    {
        private readonly TableStorageService _tableStorageService;

        public GetAllMenuItems(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                var items =
                    await _tableStorageService.GetAllMenuItemsAsync();

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(items);

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