// ST10472017
// Microsoft, 2026. Handle Azure Storage queue poison messages. [Online]
// Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger#poison-messages
// [Accessed 12 October 2026].
// Microsoft, 2026. LoggerExtensions.LogCritical Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggerextensions.logcritical
// [Accessed 12 October 2026].

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class HandlePoisonOrder
    {
        private readonly IOrderTableService _orders;
        private readonly ILogger<HandlePoisonOrder> _logger;

        public HandlePoisonOrder(IOrderTableService orders, ILogger<HandlePoisonOrder> logger)
        {
            _orders = orders;
            _logger = logger;
        }

        [Function("HandlePoisonOrder")]
        public async Task Run(
            [QueueTrigger("order-processing-queue-poison", Connection = "AzureWebJobsStorage")] string message,
            CancellationToken ct)
        {
            _logger.LogCritical("POISON MESSAGE - order could not be processed after all retries: {Message}", message);

            try
            {
                var order = JsonSerializer.Deserialize<OrderMessage>(message, JsonDefaults.options);
                if (order is null || string.IsNullOrWhiteSpace(order.OrderId) || order.OrderTimestamp == default)
                {
                    _logger.LogError("Poison message is not a readable order - kept in logs only.");
                    return;
                }

                var (entity, created) = await _orders.CreateAsync(order, OrderStatus.Failed, ct);
                _logger.LogError("Order {OrderId} recorded with status {Status} (new row: {Created})",
                    entity.RowKey, entity.Status, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not record poison message in the Orders table.");
            }
        }
    }
}
