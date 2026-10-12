using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillFunctions.Models
{
    public class CreateOrderRequest
    {
        [StringLength(40, ErrorMessage = "OrderID must be at most 40 characters long.")]
        public string? OrderID { get; set; }

        [Required(ErrorMessage = "CustomerName is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "CustomerName must be between 2 and 100 characters long.")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "SelectedItemSKUs is required.")]
        [MinLength(1, ErrorMessage = "At least one item must be selected.")]
        public List<string>? SelectedItemSKUs { get; set; }

        [Required(ErrorMessage = "TotalPrice is required.")]
        [Range(0.01, 100000.0, ErrorMessage = "TotalPrice must be between 0.01 and 100000.0.")]
        public decimal? TotalPrice { get; set; }

        public DateTimeOffset? OrderTimestamp { get; set; }
    }
}
