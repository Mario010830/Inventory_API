using System;

namespace APICore.Common.Enums
{
    public static class LoanInterestRatePeriodParsing
    {
        /// <summary>Convierte un valor de API (p. ej. "monthly"); null o vacío → annual.</summary>
        public static bool TryParse(string? value, out LoanInterestRatePeriod period)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                period = LoanInterestRatePeriod.annual;
                return true;
            }

            return Enum.TryParse(value.Trim(), ignoreCase: true, out period);
        }
    }
}
