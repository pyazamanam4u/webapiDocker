using System;

namespace WebApiDemo
{
    public static class AstroUtils
    {
        public static double NormalizeAngle(double a)
        {
            a %= 360.0;
            if (a < 0) a += 360.0;
            return a;
        }

        // Convert Julian Day (UTC) to DateTime (UTC)
        public static DateTime JulianDayToDateTimeUtc(double jd)
        {
            double J = jd + 0.5;
            double Z = Math.Floor(J);
            double F = J - Z;
            double A = Z;
            if (Z >= 2299161)
            {
                double alpha = Math.Floor((Z - 1867216.25) / 36524.25);
                A = Z + 1 + alpha - Math.Floor(alpha / 4.0);
            }
            double B = A + 1524;
            double C = Math.Floor((B - 122.1) / 365.25);
            double D = Math.Floor(365.25 * C);
            double E = Math.Floor((B - D) / 30.6001);
            double day = B - D - Math.Floor(30.6001 * E) + F;
            int dayInt = (int)Math.Floor(day);
            double dayFrac = day - dayInt;
            int month = (int)((E < 14) ? E - 1 : E - 13);
            int year = (int)((month > 2) ? C - 4716 : C - 4715);

            double hours = dayFrac * 24.0;
            int hour = (int)Math.Floor(hours);
            double mins = (hours - hour) * 60.0;
            int minute = (int)Math.Floor(mins);
            int second = (int)Math.Round((mins - minute) * 60.0);

            return new DateTime(year, month, dayInt, hour, minute, second, DateTimeKind.Utc);
        }

        public static DateOnly DateOnlyFromJulian(double jd)
        {
            return DateOnly.FromDateTime(JulianDayToDateTimeUtc(jd));
        }
    }
}