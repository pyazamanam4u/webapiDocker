
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using WebApiDemo.Models;

public interface IPanchangService
{
    PanchangResponseDto Calculate(
        DateTimeOffset dateTime,
        double latitude,
        double longitude);
}
public sealed class PanchangService : IPanchangService
{
    private readonly ISwissEphemerisService _ephemeris;

    public PanchangService(
        ISwissEphemerisService ephemeris)
    {
        _ephemeris = ephemeris;
    }

    public PanchangResponseDto Calculate(
       DateTimeOffset dateTime,
       double latitude,
       double longitude)
    {
        var utc = dateTime.UtcDateTime;

        double moonLongitude =
            _ephemeris.GetMoonLongitude(utc);

        double sunLongitude =
            _ephemeris.GetSunLongitude(utc);

        double angle =
            (moonLongitude - sunLongitude + 360) % 360;

        // Absolute Tithi 1-30
        int absoluteTithiNumber =
            (int)Math.Floor(angle / 12.0) + 1;

        string paksha =
            absoluteTithiNumber <= 15
                ? "Shukla Paksha"
                : "Krishna Paksha";

        // Display Tithi 1-15
        int displayTithiNumber =
            absoluteTithiNumber <= 15
                ? absoluteTithiNumber
                : absoluteTithiNumber - 15;

        string englishName =
            GetTithiName(displayTithiNumber);

        string teluguName =
            GetTeluguTithiName(displayTithiNumber);

        int nakshatraNumber =
            (int)Math.Floor(
                moonLongitude /
                (360.0 / 27.0)) + 1;

        string nakshatra =
            GetNakshatraName(nakshatraNumber);

        string yoga =
            CalculateYoga(
                moonLongitude,
                sunLongitude);

        string karana =
            CalculateKarana(angle);

        string moonRashi =
            CalculateMoonRashi(moonLongitude);

        string weekday =
            dateTime.ToString("dddd");

        string rutu =
            CalculateRutu(sunLongitude);

        string yearName =
            CalculateSamvatsara(dateTime.Year);

        //string sankalpaTemplate =
        //    $"Om Namo Narayanaya. " +
        //    $"Today is {dateTime:yyyy-MM-dd}, " +
        //    $"{paksha}, {englishName}.";
        string sankalpaTemplate = $@"
ॐ नमो नारायणाय ।

अद्य ब्रह्मणः द्वितीयपरार्धे
श्वेतवाराहकल्पे
वैवस्वतमन्वन्तरे
अष्टाविंशतितमे कलियुगे
प्रथमपादे
जम्बूद्वीपे
भरतवर्षे
भरतखण्डे

{yearName} नाम संवत्सरे
{rutu} ऋतौ
{paksha} पक्षे
{englishName} तिथौ
{weekday} वासरे
{nakshatra} नक्षत्रे
{yoga} योगे
{karana} करणे
{moonRashi} राशिस्थिते चन्द्रे

श्रीमन्नारायण प्रीत्यर्थं
ममोपात्त समस्त दुरितक्षयद्वारा
purpose
करिष्ये ।

ॐ तत्सत् ॥
";
        return new PanchangResponseDto(
            Date: dateTime.ToString("yyyy-MM-dd"),

            Location: new LocationDto(
                Latitude: latitude,
                Longitude: longitude),

            Sunrise: dateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),

            Weekday: weekday,

            Rutu: rutu,

            YearName: yearName,

            Tithi: new TithiDto(
                Number: displayTithiNumber,
                EnglishName: englishName,
                TeluguName: teluguName,
                Paksha: paksha,

                StartTime: "",
                EndTime: "",

                PrevailingAtSunrise: true,

                Nakshatra: nakshatra,

                Yoga: yoga,

                Karana: karana,

                MoonRashi: moonRashi

                ),
            SankalpaTemplate: sankalpaTemplate
        );
    }

    private static string CalculateMoonRashi(
    double moonLongitude)
    {
        string[] rashis =
        {
        "Mesha",
        "Vrishabha",
        "Mithuna",
        "Karka",
        "Simha",
        "Kanya",
        "Tula",
        "Vrischika",
        "Dhanu",
        "Makara",
        "Kumbha",
        "Meena"
    };

        int index =
            (int)Math.Floor(moonLongitude / 30.0);

        return rashis[index];
    }
    
    private static string CalculateSamvatsara(int year)
    {
        string[] samvatsaras =
        {
        "Prabhava",
        "Vibhava",
        "Shukla",
        "Pramoda",
        "Prajotpatti",
        "Angirasa",
        "Shrimukha",
        "Bhava",
        "Yuva",
        "Dhatu",
        "Ishvara",
        "Bahudhanya",
        "Pramathi",
        "Vikrama",
        "Vrisha",
        "Chitrabhanu",
        "Subhanu",
        "Tara",
        "Parthiva",
        "Vyaya",
        "Sarvajit",
        "Sarvadhari",
        "Virodhi",
        "Vikriti",
        "Khara",
        "Nandana",
        "Vijaya",
        "Jaya",
        "Manmatha",
        "Durmukha",
        "Hevilambi",
        "Vilambi",
        "Vikari",
        "Sharvari",
        "Plava",
        "Shubhakruthu",
        "Shobhakruthu",
        "Krodhi",
        "Vishvavasu",
        "Parabhava",
        "Plavanga",
        "Kilaka",
        "Saumya",
        "Sadharana",
        "Virodhikruthu",
        "Paritapitha",
        "Pramadhisha",
        "Ananda",
        "Rakshasa",
        "Nala",
        "Pingala",
        "Kalayukthi",
        "Siddharthi",
        "Raudra",
        "Durmathi",
        "Dundubhi",
        "Rudhirodgari",
        "Raktakshi",
        "Krodhana",
        "Akshaya"
    };

        int baseYear = 1987;

        int index =
            ((year - baseYear) % 60 + 60) % 60;

        return samvatsaras[index];
    }

    private static string GetTithiName(int number)
    {
        string[] names =
        {
            "Pratipada",
            "Dvitiya",
            "Tritiya",
            "Chaturthi",
            "Panchami",
            "Shashti",
            "Saptami",
            "Ashtami",
            "Navami",
            "Dashami",
            "Ekadashi",
            "Dwadashi",
            "Trayodashi",
            "Chaturdashi",
            "Purnima/Amavasya"
        };

        return names[number - 1];
    }


    private static string GetTeluguTithiName(int tithiNumber)
    {
        return tithiNumber switch
        {
            1 => "పాడ్యమి",
            2 => "విదియ",
            3 => "తదియ",
            4 => "చవితి",
            5 => "పంచమి",
            6 => "షష్ఠి",
            7 => "సప్తమి",
            8 => "అష్టమి",
            9 => "నవమి",
            10 => "దశమి",
            11 => "ఏకాదశి",
            12 => "ద్వాదశి",
            13 => "త్రయోదశి",
            14 => "చతుర్దశి",
            15 => "పౌర్ణమి / అమావాస్య",
            _ => "తెలియదు"
        };
    }
    private static string GetNakshatraName(int number)
    {
        string[] names =
        {
            "Ashwini",
            "Bharani",
            "Krittika",
            "Rohini",
            "Mrigashira",
            "Ardra",
            "Punarvasu",
            "Pushya",
            "Ashlesha",
            "Magha",
            "Purva Phalguni",
            "Uttara Phalguni",
            "Hasta",
            "Chitra",
            "Swati",
            "Vishakha",
            "Anuradha",
            "Jyeshta",
            "Mula",
            "Purva Ashadha",
            "Uttara Ashadha",
            "Shravana",
            "Dhanishta",
            "Shatabhisha",
            "Purva Bhadrapada",
            "Uttara Bhadrapada",
            "Revati"
        };

        return names[number - 1];
    }

    private static string CalculateYoga(
        double moon,
        double sun)
    {
        double value =
            (moon + sun) % 360;

        int yoga =
            (int)Math.Floor(value /
            (360.0 / 27.0));

        return $"Yoga {yoga + 1}";
    }

    private static string CalculateKarana(
        double angle)
    {
        int karana =
            (int)Math.Floor(angle / 6.0);

        return $"Karana {karana + 1}";
    }

    private static string CalculateRutu(double sunLongitude)
    {
        int rashiIndex = (int)Math.Floor(sunLongitude / 30.0);

        return rashiIndex switch
        {
            0 or 1 => "Vasanta",
            2 or 3 => "Greeshma",
            4 or 5 => "Varsha",
            6 or 7 => "Sharad",
            8 or 9 => "Hemanta",
            10 or 11 => "Shishira",
            _ => "Unknown"
        };
    }
}