using SwissEphNet;

public interface ISwissEphemerisService
{
    double GetSunLongitude(DateTime utcDateTime);
    double GetMoonLongitude(DateTime utcDateTime);
}

public sealed class SwissEphemerisService : ISwissEphemerisService
{
    private readonly SwissEph _swe;

    public SwissEphemerisService()
    {
        _swe = new SwissEph();

        _swe.swe_set_ephe_path("ephe");
    }

    private double ToJulianDay(DateTime utc)
    {
        return _swe.swe_julday(
            utc.Year,
            utc.Month,
            utc.Day,
            utc.Hour +
            utc.Minute / 60.0 +
            utc.Second / 3600.0,
            SwissEph.SE_GREG_CAL);
    }
    public double GetSunLongitude(DateTime utc)
    {
        double jd = ToJulianDay(utc);

        double[] xx = new double[6];
        string serr = string.Empty;

        _swe.swe_calc_ut(
            jd,
            SwissEph.SE_SUN,
            SwissEph.SEFLG_SWIEPH,
            xx,
            ref serr);

        return Normalize(xx[0]);
    }

    public double GetMoonLongitude(DateTime utc)
    {
        double jd = ToJulianDay(utc);

        double[] xx = new double[6];
        string serr = string.Empty;

        _swe.swe_calc_ut(
            jd,
            SwissEph.SE_MOON,
            SwissEph.SEFLG_SWIEPH,
            xx,
            ref serr);

        return Normalize(xx[0]);
    }

    private static double Normalize(double angle)
    {
        angle %= 360;

        if (angle < 0)
            angle += 360;

        return angle;
    }
}