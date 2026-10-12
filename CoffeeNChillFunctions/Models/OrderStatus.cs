// ST10472017
// Microsoft, 2026. String.Equals Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.string.equals
// [Accessed 12 October 2026].
// Microsoft, 2026. Array.IndexOf Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.array.indexof
// [Accessed 12 October 2026].

using System;
using System.Linq;

namespace CoffeeNChillFunctions.Models
{
    public static class OrderStatus
    {
        public const string Received = "Received";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string Collected = "Collected";
        public const string Failed = "Failed";

        public static readonly string[] Lifecycle = { Received, Preparing, Ready, Collected };

        public static string? Normalize(string? value) =>
            Lifecycle.FirstOrDefault(s => s.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));

        public static bool IsValidTransition(string current, string next)
        {
            var from = Array.IndexOf(Lifecycle, current);
            var to = Array.IndexOf(Lifecycle, next);
            return from >= 0 && to == from + 1;
        }
    }
}
