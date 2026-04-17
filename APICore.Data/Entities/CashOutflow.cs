using System;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    /// <summary>Retiro o salida manual de efectivo de caja, asociado a una fecha contable y ubicación.</summary>
    public class CashOutflow : BaseEntity
    {
        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public int LocationId { get; set; }

        /// <summary>Fecha contable del retiro (solo la parte fecha).</summary>
        [Required]
        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        public int? UserId { get; set; }

        public Organization? Organization { get; set; }
        public Location? Location { get; set; }
        public User? User { get; set; }
    }
}
