using System;
using APICore.Common.Constants;

namespace APICore.Common.Helpers
{
    /// <summary>
    /// Cálculo de días restantes para la API. Evita mostrar ~36.000 días cuando EndDate está en +100 años
    /// pero el ciclo es mensual/anual (datos inconsistentes o plan free con vigencia larga).
    /// </summary>
    public static class SubscriptionDisplayHelper
    {
        /// <summary>
        /// -1 = sin límite práctico solo si el plan free sigue con EndDate muy lejano (datos antiguos +100 años).
        /// Con periodo mensual real en BD, se calculan días hasta EndDate como el resto.
        /// </summary>
        public static int ComputeDaysRemaining(
            DateTime startDate,
            DateTime endDate,
            string? billingCycle,
            string? planName,
            DateTime utcNow)
        {
            var isFree = !string.IsNullOrEmpty(planName)
                && string.Equals(planName, PlanNames.Free, StringComparison.OrdinalIgnoreCase);
            if (isFree && endDate > utcNow.AddYears(2))
                return -1;

            var cycle = billingCycle?.Trim().ToLowerInvariant();
            var useRollingRenewal = !string.IsNullOrEmpty(cycle)
                && (cycle == BillingCycle.Monthly || cycle == BillingCycle.Annual)
                && endDate > utcNow.AddYears(2);

            if (!useRollingRenewal)
                return (int)Math.Max(0, Math.Ceiling((endDate - utcNow).TotalDays));

            var next = startDate;
            if (cycle == BillingCycle.Annual)
            {
                while (next <= utcNow)
                    next = next.AddYears(1);
            }
            else
            {
                while (next <= utcNow)
                    next = next.AddMonths(1);
            }

            return (int)Math.Max(0, Math.Ceiling((next - utcNow).TotalDays));
        }
    }
}
