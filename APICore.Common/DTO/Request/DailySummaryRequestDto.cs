using System;

namespace APICore.Common.DTO.Request
{
    public class DailySummaryRequestDto
    {
        /// <summary>Día contable en Cuba (fecha civil; el backend calcula 00:00–24:00 en zona America/Havana).</summary>
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
