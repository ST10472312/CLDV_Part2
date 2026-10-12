// ST10472017
// Microsoft, 2026. Records (C# reference). [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
// [Accessed 12 October 2026].
// Microsoft, 2026. String.Split Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.string.split
// [Accessed 12 October 2026].

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoffeeNChillFunctions.Models
{
    public record OrderResponse(
        string OrderId,
        string OrderDate,
        string CustomerName,
        List<string> SelectedItemSKUs,
        double TotalPrice,
        DateTimeOffset OrderTimestamp,
        string Status,
        DateTimeOffset LastUpdated)
    {
        public static OrderResponse FromEntity(OrderEntity e) => new(
            e.RowKey,
            e.PartitionKey,
            e.CustomerName,
            e.SelectedItemSKUs
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            e.TotalPrice,
            e.OrderTimestamp,
            e.Status,
            e.LastUpdated);
    }
}
