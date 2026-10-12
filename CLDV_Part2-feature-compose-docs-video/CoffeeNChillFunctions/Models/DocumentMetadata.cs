using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeNChillFunctions.Models
{
    public record DocumentMetadata(
        string fileName,
        long SizeBytes,
        DateTimeOffset LastModified,
        string ContentType)
    {
        public string SizeDisplay =>
            SizeBytes < 1024
                ? $"{SizeBytes} B"
                : SizeBytes < 1024 * 1024
                    ? $"{SizeBytes / 1024.0:F1} KB"
                    : $"{SizeBytes / (1024.0 * 1024.0):F1} MB";
                    

    }
}
