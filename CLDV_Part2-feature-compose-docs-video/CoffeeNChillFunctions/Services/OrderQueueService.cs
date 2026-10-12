using Azure.Storage.Queues;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoffeeNChillFunctions.Services
{
    public interface IOrderQueueService
    {
        Task EnqueueAsync(OrderMessage order, CancellationToken ct = default);
    }

    public class OrderQueueService : IOrderQueueService
    {
        public const string QueueName = "order-processing-queue";
        private readonly QueueClient _queue;
        private readonly ILogger<OrderQueueService> _logger;

        public OrderQueueService(QueueServiceClient serviceClient, ILogger<OrderQueueService> logger)
        {
            _queue = serviceClient.GetQueueClient(QueueName);
            _logger = logger;
        }

        public async Task EnqueueAsync(OrderMessage order, CancellationToken ct = default)
        {
            await _queue.CreateIfNotExistsAsync(cancellationToken: ct);

            var json = JsonSerializer.Serialize(order, JsonDefaults.options);

            await _queue.SendMessageAsync(json, ct);

            _logger.LogInformation("Order {OrderId} placed on {Queue}", order.OrderId, QueueName);
        }
    }
}
