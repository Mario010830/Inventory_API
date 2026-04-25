using System;

namespace APICore.Common.DTO.Request
{
    public class DailySummaryExportRequestDto
    {
        public DateTime Date { get; set; }

        /// <summary>Requerido si el usuario no tiene ubicación fija (admin): misma regla que GET cuadre.</summary>
        public int? LocationId { get; set; }

        /// <summary>Si se indica, exporta ese cuadre concreto (varios por día). Si null, el último cerrado de la fecha.</summary>
        public int? Id { get; set; }
    }
}
