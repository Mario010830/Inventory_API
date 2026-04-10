using System;

namespace APICore.Common.DTO.Request
{
    public class DailySummaryRequestDto
    {
        public DateTime Date { get; set; }

        /// <summary>
        /// Requerido cuando el usuario es Admin de organización (sin localización fija).
        /// Si el usuario ya tiene una localización asignada en su perfil, este campo se ignora.
        /// </summary>
        public int? LocationId { get; set; }

        public decimal OpeningCash { get; set; }
        public decimal ActualCash { get; set; }
        public string? Notes { get; set; }
    }
}
