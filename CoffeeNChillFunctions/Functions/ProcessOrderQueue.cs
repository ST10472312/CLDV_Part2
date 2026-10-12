// ST10472017
// Microsoft, 2026. Azure Queue storage trigger for Azure Functions. [Online]
// Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger
// [Accessed 12 October 2026].
// Microsoft, 2026. Handle Azure Storage queue poison messages. [Online]
// Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger#poison-messages
// [Accessed 12 October 2026].
// Microsoft, 2026. FunctionContext.BindingContext Property. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.functioncontext.bindingcontext
// [Accessed 12 October 2026].
// Stack Overflow, 2023. How to get the dequeue count inside an Azure Functions isolated worker queue trigger. [Online]
// Available at: https://stackoverflow.com/questions/70543461
// [Accessed 12 October 2026].

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class ProcessOrderQueue
    {
        private readonly IOrderTableService _orders;
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(IOrderTableService orders, ILogger<ProcessOrderQueue> logger)
        {
            _orders = orders;
            _logger = logger;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string message,
            FunctionContext context,
            CancellationToken ct)
        {
            var dequeueCount = context.BindingContext.BindingData.TryGetValue("DequeueCount", out var dc) ? dc : "?";
            _logger.LogInformation("ProcessOrderQueue triggered (attempt {Attempt}): {Message}", dequeueCount, message);

            try
            {
                var order = JsonSerializer.Deserialize<OrderMessage>(message, JsonDefaults.options)
                    ?? throw new InvalidDataException("Queue message deserialized to null.");

                ValidateOrFail(order);

                if (Environment.GetEnvironmentVariable("ENABLE_POISON_TEST") == "true" &&
                    order.CustomerName.Equals("POISON-TEST", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Simulated processing failure for poison-queue demo.");
                }

                var (entity, created) = await _orders.CreateAsync(order, OrderStatus.Received, ct);
                _logger.LogInformation("Order {OrderId} stored in Orders table (created: {Created}), status {Status}",
                    entity.RowKey, created, entity.Status);

                if (created)
                    await SimulateLifecycleAsync(entity, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed on attempt {Attempt}. Message: {Message}", dequeueCount, message);
                throw;
            }
        }

        private static void ValidateOrFail(OrderMessage order)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(order.OrderId)) problems.Add("OrderId missing");
            if (string.IsNullOrWhiteSpace(order.CustomerName)) problems.Add("CustomerName missing");
            if (order.SelectedItemSKUs.Count == 0) problems.Add("SelectedItemSKUs empty");
            if (order.TotalPrice <= 0) problems.Add("TotalPrice must be > 0");
            if (order.OrderTimestamp == default) problems.Add("OrderTimestamp missing");

            if (problems.Count > 0)
                throw new ValidationException("Invalid order message: " + string.Join("; ", problems));
        }

        private async Task SimulateLifecycleAsync(OrderEntity entity, CancellationToken ct)
        {
            var delaySeconds = int.TryParse(Environment.GetEnvironmentVariable("STATUS_DELAY_SECONDS"), out var d) ? d : 8;
            if (delaySeconds <= 0) return;

            var expectedCurrent = OrderStatus.Received;
            foreach (var next in OrderStatus.Lifecycle.Skip(1))
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);

                var latest = await _orders.GetAsync(entity.PartitionKey, entity.RowKey, ct);
                if (latest is null || latest.Status != expectedCurrent)
                {
                    _logger.LogInformation("Order {OrderId} was changed manually - stopping simulation", entity.RowKey);
                    return;
                }

                if (!await _orders.UpdateStatusAsync(latest, next, ct))
                    return;

                _logger.LogInformation("Order {OrderId}: {From} -> {To}", entity.RowKey, expectedCurrent, next);
                expectedCurrent = next;
            }
        }
    }
}
