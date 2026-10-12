using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeNChillFunctions.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<string> SelectedItemSKUs { get; set; } = new List<string>();
        public decimal TotalPrice { get; set; }
        public DateTimeOffset OrderTimestamp { get; set; }
    }
}
