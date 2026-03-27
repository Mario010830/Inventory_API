using System;

namespace APICore.Common.DTO.Response
{
    public class SubscriptionResponse
    {
        public int Id { get; set; }
        public string BillingCycle { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PlanResponse? Plan { get; set; }
        /// <summary>Poblado en listados/detalle admin; puede ser null en otros contextos.</summary>
        public OrganizationResponse? Organization { get; set; }
        /// <summary>Contacto mínimo del admin de la organización (si existe).</summary>
        public SubscriptionAdminContactResponse? AdminContact { get; set; }
        /// <summary>Días hasta el fin del periodo actual. -1 = ilimitado (plan free).</summary>
        public int DaysRemaining { get; set; }
    }
}
