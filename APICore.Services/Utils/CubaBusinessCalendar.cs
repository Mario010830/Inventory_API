using System;

namespace APICore.Services.Utils
{
    /// <summary>
    /// Límites de día / semana / mes / año según calendario civil en Cuba (IANA <c>America/Havana</c> o Windows <c>Cuba Standard Time</c>).
    /// Los instantes en BD siguen en UTC; aquí solo se traducen rangos de consulta y agrupaciones.
    /// </summary>
    public static class CubaBusinessCalendar
    {
        private static readonly Lazy<TimeZoneInfo> CubaTz = new(ResolveCubaTimeZone);

        public static TimeZoneInfo CubaTimeZone => CubaTz.Value;

        private static TimeZoneInfo ResolveCubaTimeZone()
        {
            foreach (var id in new[] { "America/Havana", "Cuba Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            throw new InvalidOperationException(
                "No se encontró la zona horaria de Cuba (America/Havana / Cuba Standard Time).");
        }

        /// <summary>Inicio del día civil en Cuba (UTC inclusive) y fin exclusivo (inicio del día siguiente en Cuba).</summary>
        public static (DateTime fromUtcInclusive, DateTime toUtcExclusive) GetCubaCalendarDayRangeUtc(DateTime utcNow)
        {
            var nowCuba = TimeZoneInfo.ConvertTimeFromUtc(utcNow, CubaTimeZone);
            var d = nowCuba.Date;
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), CubaTimeZone);
            return (fromUtc, fromUtc.AddDays(1));
        }

        public static (DateTime fromUtcInclusive, DateTime toUtcExclusive) GetCubaWeekRangeUtc(DateTime utcNow)
        {
            var nowCuba = TimeZoneInfo.ConvertTimeFromUtc(utcNow, CubaTimeZone);
            var d = nowCuba.Date;
            var dow = (int)d.DayOfWeek;
            var daysFromMonday = dow == 0 ? 6 : dow - (int)DayOfWeek.Monday;
            var mondayCuba = d.AddDays(-daysFromMonday);
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(mondayCuba, DateTimeKind.Unspecified), CubaTimeZone);
            return (fromUtc, fromUtc.AddDays(7));
        }

        public static (DateTime fromUtcInclusive, DateTime toUtcExclusive) GetCubaMonthRangeUtc(DateTime utcNow)
        {
            var nowCuba = TimeZoneInfo.ConvertTimeFromUtc(utcNow, CubaTimeZone);
            var d = nowCuba.Date;
            var first = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(first, CubaTimeZone);
            var nextMonth = first.AddMonths(1);
            var toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(nextMonth, CubaTimeZone);
            return (fromUtc, toUtcExclusive);
        }

        public static (DateTime fromUtcInclusive, DateTime toUtcExclusive) GetCubaYearRangeUtc(DateTime utcNow)
        {
            var nowCuba = TimeZoneInfo.ConvertTimeFromUtc(utcNow, CubaTimeZone);
            var y = nowCuba.Year;
            var first = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(first, CubaTimeZone);
            var nextYear = new DateTime(y + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(nextYear, CubaTimeZone);
            return (fromUtc, toUtcExclusive);
        }

        /// <summary>
        /// Rango UTC para gráficos de movimientos. Si <paramref name="from"/> y <paramref name="to"/> tienen valor,
        /// se interpretan como fechas civiles en Cuba (00:00 del desde hasta 24:00 del hasta).
        /// </summary>
        public static (DateTime startUtc, DateTime endUtcExclusive, DateTime loopStartCubaDate, DateTime loopEndCubaDate) ResolveMovementFlowRange(int days, DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue)
            {
                var f = from.Value.Date;
                var t = to.Value.Date;
                if (f > t)
                    f = t.AddDays(-days);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(f, DateTimeKind.Unspecified), CubaTimeZone);
                var endUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(t.AddDays(1), DateTimeKind.Unspecified), CubaTimeZone);
                if (startUtc >= endUtcExclusive)
                    return ResolveMovementFlowRange(days, null, null);

                return (startUtc, endUtcExclusive, f, t);
            }

            var (todayStart, todayEndExclusive) = GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
            var startUtcDefault = todayStart.AddDays(-days);
            var nowCuba = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CubaTimeZone);
            var loopEnd = nowCuba.Date;
            var loopStart = TimeZoneInfo.ConvertTimeFromUtc(startUtcDefault, CubaTimeZone).Date;
            return (startUtcDefault, todayEndExclusive, loopStart, loopEnd);
        }
    }
}
