// ST10472017
// Microsoft, 2026. System.ComponentModel.DataAnnotations Namespace. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
// [Accessed 12 October 2026].

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillFunctions.Models
{
    public class UpdateStatusRequest
    {
        [Required(ErrorMessage = "OrderId is required.")]
        public string? OrderId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string? Status { get; set; }
    }
}
