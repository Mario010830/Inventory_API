using System;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateCashOutflowRequest
    {
        /// <summary>Fecha contable del retiro (solo se usa la parte fecha).</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>Obligatorio si el usuario no tiene ubicación fija (admin).</summary>
        public int? LocationId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
