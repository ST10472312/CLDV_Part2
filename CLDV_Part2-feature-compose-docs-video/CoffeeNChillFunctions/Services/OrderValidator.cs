using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CoffeeNChillFunctions.Models;
using Azure.Core;

namespace CoffeeNChillFunctions.Services
{
    public static class OrderValidator
    {
        private static readonly Regex SkuPattern = new(@"^[A-Z]{3}-\d{3}$", RegexOptions.Compiled);
        private static readonly Regex OrderIdPattern = new(@"^ORD-\d{4}-[A-Z0-9]{4,8}$", RegexOptions.Compiled);
        private const int MaxItemsPerOrder = 50;
        public static bool TryValidate(CreateOrderRequest request, out List<string> errors)
        {
            errors = new List<string>();
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

            if (!string.IsNullOrWhiteSpace(request.OrderID) && !OrderIdPattern.IsMatch(request.OrderID))
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Inavlid value."));

            if (!string.IsNullOrWhiteSpace(request.OrderID) && !OrderIdPattern.IsMatch(request.OrderID))
                errors.Add("OrderId must look like ORD-2026-8801.");

            if(request.SelectedItemSKUs is { Count: > 0} skus)
            {
                if(skus.Count > MaxItemsPerOrder)
                    errors.Add($"An order cannot contain more than {MaxItemsPerOrder} items.");

                foreach (var sku in skus.Where(s => string.IsNullOrWhiteSpace(s) || !SkuPattern.IsMatch(s)))
                    errors.Add($"Invalid SKU '{sku}'. Must be in the format 'COF-001'.");
            }

            if (request.TotalPrice is { } price && decimal.Round(price, 2) != price)
                errors.Add("TotalPrice must have at most 2 decimal places.");

            return errors.Count == 0;
        }
    }
}
