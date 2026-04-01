namespace APICore.Common.Constants
{
    public static class SubscriptionStatus
    {
        public const string Active = "active";
        public const string Pending = "pending";
        /// <summary>Solicitud de plan de pago rechazada por admin; la organización sigue inactiva.</summary>
        public const string Rejected = "rejected";
        public const string Expired = "expired";
        public const string Cancelled = "cancelled";
    }
}
