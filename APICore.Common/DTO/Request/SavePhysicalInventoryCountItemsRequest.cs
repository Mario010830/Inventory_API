using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class SavePhysicalInventoryCountItemsRequest
    {
        [Required]
        [MinLength(1)]
        public List<PhysicalInventoryCountItemLineRequest> Items { get; set; } = new();
    }

    public class PhysicalInventoryCountItemLineRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal CountedQuantity { get; set; }
    }
}
