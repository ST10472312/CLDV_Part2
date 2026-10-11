using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Azure;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class QueueOrderFunctions
    {
        private readonly IOrderQueueService _queueService;
        private readonly ILogger<QueueOrderFunctions> _logger;

        public QueueOrderFunctions(IOrderQueueService queueService, ILogger<QueueOrderFunctions> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        [Function("QueueOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequest req,
            CancellationToken ct)
        {
            CreateOrderRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<CreateOrderRequest>(req.Body, JsonDefaults.options, ct);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize request body.");
                return new BadRequestObjectResult(new { error = "Invalid JSON format." });
            }

            if (body is null)
                return new BadRequestObjectResult(new { error = "Request body is empty or invalid." }); 

            if (!OrderValidator.TryValidate(body, out var errors))
            {
                _logger.LogWarning("Rejected order: {Count} validation error(s).", errors.Count);
                return new BadRequestObjectResult(new { error = "Validation failed.", details = errors });
            }

            var order = new OrderMessage
            {
                OrderId = string.IsNullOrWhiteSpace(body.OrderID)
                    ? $"ORD-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}"
                    : body.OrderID!.Trim(),
                CustomerName = body.CustomerName!.Trim(),
                SelectedItemSKUs = body.SelectedItemSKUs!.Select(s => s.Trim()).ToList(),
                TotalPrice = decimal.Round(body.TotalPrice!.Value, 2),
                OrderTimestamp = body.OrderTimestamp ?? DateTimeOffset.UtcNow
            };

            try
            {
                await _queueService.EnqueueAsync(order, ct);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Queue unavailable while placing order {OrderId}", order.OrderId);
                return new ObjectResult(new { error = "The order queue is temporarily unavailable. Please try again." })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }

            return new ObjectResult(new
            {
                orderId = order.OrderId,
                status = "Queued",
                queuedAt = order.OrderTimestamp,
                message = "Order accepted and queued for processing."
            })
            {
                StatusCode = StatusCodes.Status202Accepted
            };
        }
    }
}
