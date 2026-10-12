// ST10472017
// Microsoft, 2026. Azure Tables client library for .NET. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
// [Accessed 12 October 2026].
// Microsoft, 2026. ITableEntity Interface. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.itableentity
// [Accessed 12 October 2026].

using System;
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string SelectedItemSKUs { get; set; } = string.Empty;
        public double TotalPrice { get; set; }
        public DateTimeOffset OrderTimestamp { get; set; }
        public string Status { get; set; } = OrderStatus.Received;
        public DateTimeOffset LastUpdated { get; set; }
    }
}
