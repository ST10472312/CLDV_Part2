// ST10472017
// Microsoft, 2026. ASP.NET Core integration in .NET Isolated Azure Functions. [Online]
// Available at: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#aspnet-core-integration
// [Accessed 12 October 2026].
// Microsoft, 2026. Validator.TryValidateObject Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validator.tryvalidateobject
// [Accessed 12 October 2026].
// Microsoft, 2026. ConflictObjectResult Class. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.conflictobjectresult
// [Accessed 12 October 2026].

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class OrderStatusFunctions
    {
        private readonly IOrderTableService _orders;
        private readonly ILogger<OrderStatusFunctions> _logger;

        public OrderStatusFunctions(IOrderTableService orders, ILogger<OrderStatusFunctions> logger)
        {
            _orders = orders;
            _logger = logger;
        }

        [Function("GetAllOrders")]
        public async Task<IActionResult> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequest req,
            CancellationToken ct)
        {
            string? status = req.Query["status"];
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = OrderStatus.Normalize(status)
                    ?? (status.Equals(OrderStatus.Failed, StringComparison.OrdinalIgnoreCase)
                        ? OrderStatus.Failed
                        : null);

                if (status is null)
                    return new BadRequestObjectResult(new
                    {
                        error = "Unknown status filter. Use Received, Preparing, Ready, Collected or Failed."
                    });
            }

            var orders = await _orders.ListAsync(status, ct);
            return new OkObjectResult(orders.Select(OrderResponse.FromEntity));
        }

        [Function("GetOrderById")]
        public async Task<IActionResult> GetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequest req,
            string orderId,
            CancellationToken ct)
        {
            var entity = await _orders.FindByOrderIdAsync(orderId, ct);
            return entity is null
                ? new NotFoundObjectResult(new
                {
                    error = $"Order '{orderId}' not found (it may still be waiting in the queue)."
                })
                : new OkObjectResult(OrderResponse.FromEntity(entity));
        }

        [Function("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/status")] HttpRequest req,
            CancellationToken ct)
        {
            UpdateStatusRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<UpdateStatusRequest>(req.Body, JsonDefaults.options, ct);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new { error = "Request body is empty or is not valid JSON." });
            }

            var results = new List<ValidationResult>();
            if (body is null || !Validator.TryValidateObject(body, new ValidationContext(body), results, true))
                return new BadRequestObjectResult(new
                {
                    error = "Validation failed.",
                    details = results.Select(r => r.ErrorMessage)
                });

            var newStatus = OrderStatus.Normalize(body.Status);
            if (newStatus is null)
                return new BadRequestObjectResult(new
                {
                    error = "Status must be Received, Preparing, Ready or Collected."
                });

            var entity = await _orders.FindByOrderIdAsync(body.OrderId!, ct);
            if (entity is null)
                return new NotFoundObjectResult(new { error = $"Order '{body.OrderId}' not found." });

            if (!OrderStatus.IsValidTransition(entity.Status, newStatus))
                return new ConflictObjectResult(new
                {
                    error = $"Invalid transition {entity.Status} -> {newStatus}. " +
                            $"Next allowed status is the one directly after '{entity.Status}'."
                });

            if (!await _orders.UpdateStatusAsync(entity, newStatus, ct))
                return new ConflictObjectResult(new
                {
                    error = "The order was changed by someone else. Reload it and try again."
                });

            _logger.LogInformation("Order {OrderId} manually set to {Status}", entity.RowKey, newStatus);
            return new OkObjectResult(OrderResponse.FromEntity(entity));
        }
    }
}
