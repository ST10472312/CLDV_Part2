using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CoffeeNChillFunctions.Models
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }
}
