using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public class TableStorageService
    {
        private readonly TableClient _tableClient;

        public TableStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage configuration is not set");

            string tableName = "MenuItems";

            _tableClient = new TableClient(connectionString, tableName);

            _tableClient.CreateIfNotExists();
        }

        public async Task AddMenuItemAsync(MenuItem item)
        {
            await _tableClient.AddEntityAsync(item);
        }

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            List<MenuItem> items = new List<MenuItem>();

            await foreach (MenuItem item in _tableClient.QueryAsync<MenuItem>())
            {
                items.Add(item);
            }

            return items;
        }

        public async Task<MenuItem?> GetMenuItemAsync(string category, string id)
        {
            try
            {
                Response<MenuItem> response =
                    await _tableClient.GetEntityAsync<MenuItem>(category, id);

                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category)
        {
            List<MenuItem> items = new List<MenuItem>();

            await foreach (MenuItem item in
                _tableClient.QueryAsync<MenuItem>(
                    x => x.PartitionKey == category))
            {
                items.Add(item);
            }

            return items;
        }

        public async Task UpdateMenuItemAsync(MenuItem item)
        {
            await _tableClient.UpdateEntityAsync(
                item,
                ETag.All,
                TableUpdateMode.Replace);
        }

        public async Task DeleteMenuItemAsync(string category, string id)
        {
            await _tableClient.DeleteEntityAsync(category, id);
        }
    }
}