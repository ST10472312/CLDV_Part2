// ST10472017
// Microsoft, 2026. Azure Tables client library for .NET. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
// [Accessed 12 October 2026].
// Microsoft, 2026. TableClient.CreateQueryFilter Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.createqueryfilter
// [Accessed 12 October 2026].
// Microsoft, 2026. Manage concurrency in Blob storage and Table storage (ETag). [Online]
// Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/concurrency-manage
// [Accessed 12 October 2026].
// Stack Overflow, 2024. How to handle 409 Conflict when inserting an entity into Azure Table Storage. [Online]
// Available at: https://stackoverflow.com/questions/22519573
// [Accessed 12 October 2026].

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Services
{
    public interface IOrderTableService
    {
        Task<(OrderEntity Entity, bool Created)> CreateAsync(
            OrderMessage order, string initialStatus = OrderStatus.Received, CancellationToken ct = default);
        Task<OrderEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken ct = default);
        Task<OrderEntity?> FindByOrderIdAsync(string orderId, CancellationToken ct = default);
        Task<List<OrderEntity>> ListAsync(string? status = null, CancellationToken ct = default);
        Task<bool> UpdateStatusAsync(OrderEntity entity, string newStatus, CancellationToken ct = default);
    }

    public class OrderTableService : IOrderTableService
    {
        public const string TableName = "Orders";

        private readonly TableClient _table;
        private readonly ILogger<OrderTableService> _logger;
        private bool _tableEnsured;

        public OrderTableService(TableServiceClient serviceClient, ILogger<OrderTableService> logger)
        {
            _table = serviceClient.GetTableClient(TableName);
            _logger = logger;
        }

        private async Task EnsureTableAsync(CancellationToken ct)
        {
            if (_tableEnsured) return;
            await _table.CreateIfNotExistsAsync(ct);
            _tableEnsured = true;
        }

        public async Task<(OrderEntity Entity, bool Created)> CreateAsync(
            OrderMessage order, string initialStatus = OrderStatus.Received, CancellationToken ct = default)
        {
            await EnsureTableAsync(ct);

            var entity = new OrderEntity
            {
                PartitionKey = order.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd"),
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                SelectedItemSKUs = string.Join(",", order.SelectedItemSKUs),
                TotalPrice = (double)order.TotalPrice,
                OrderTimestamp = order.OrderTimestamp,
                Status = initialStatus,
                LastUpdated = DateTimeOffset.UtcNow
            };

            try
            {
                await _table.AddEntityAsync(entity, ct);
                return (entity, true);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                _logger.LogWarning("Order {OrderId} already exists - treating redelivery as success", order.OrderId);
                var existing = await GetAsync(entity.PartitionKey, entity.RowKey, ct);
                return (existing ?? entity, false);
            }
        }

        public async Task<OrderEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken ct = default)
        {
            await EnsureTableAsync(ct);
            var response = await _table.GetEntityIfExistsAsync<OrderEntity>(partitionKey, rowKey, cancellationToken: ct);
            return response.HasValue ? response.Value : null;
        }

        public async Task<OrderEntity?> FindByOrderIdAsync(string orderId, CancellationToken ct = default)
        {
            await EnsureTableAsync(ct);
            var filter = TableClient.CreateQueryFilter($"RowKey eq {orderId}");
            await foreach (var entity in _table.QueryAsync<OrderEntity>(filter, cancellationToken: ct))
                return entity;
            return null;
        }

        public async Task<List<OrderEntity>> ListAsync(string? status = null, CancellationToken ct = default)
        {
            await EnsureTableAsync(ct);
            var results = new List<OrderEntity>();
            await foreach (var entity in _table.QueryAsync<OrderEntity>(cancellationToken: ct))
            {
                if (status is null || entity.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    results.Add(entity);
            }
            return results.OrderByDescending(o => o.OrderTimestamp).ToList();
        }

        public async Task<bool> UpdateStatusAsync(OrderEntity entity, string newStatus, CancellationToken ct = default)
        {
            entity.Status = newStatus;
            entity.LastUpdated = DateTimeOffset.UtcNow;
            try
            {
                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, ct);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                _logger.LogWarning("Order {OrderId} was changed by someone else - status update skipped", entity.RowKey);
                return false;
            }
        }
    }
}
