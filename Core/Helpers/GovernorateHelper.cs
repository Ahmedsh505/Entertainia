using Core.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Core.Helpers
{
    /// <summary>
    /// Helper class for governorate operations including coordinate-based detection
    /// </summary>
    public static class GovernorateHelper
    {
        /// <summary>
        /// Approximate center coordinates for each governorate (for reference)
        /// </summary>
        private static readonly Dictionary<Governorate, (double Lat, double Lon)> GovernorateCoordinates = new()
        {
            { Governorate.Alexandria, (31.2001, 29.9187) },
            { Governorate.Aswan, (24.0889, 32.8998) },
            { Governorate.Asyut, (27.1809, 31.1837) },
            { Governorate.Beheira, (30.8481, 30.3435) },
            { Governorate.BeniSuef, (29.0661, 31.0994) },
            { Governorate.Cairo, (30.0444, 31.2357) },
            { Governorate.Dakahlia, (31.1656, 31.4913) },
            { Governorate.Damietta, (31.4175, 31.8144) },
            { Governorate.Faiyum, (29.3084, 30.8428) },
            { Governorate.Gharbia, (30.8754, 31.0335) },
            { Governorate.Giza, (30.0131, 31.2089) },
            { Governorate.Ismailia, (30.5833, 32.2667) },
            { Governorate.KafrElSheikh, (31.1107, 30.9388) },
            { Governorate.Luxor, (25.6872, 32.6396) },
            { Governorate.Matrouh, (31.3543, 27.2373) },
            { Governorate.Minya, (28.0871, 30.7618) },
            { Governorate.Monufia, (30.5972, 30.9876) },
            { Governorate.NewValley, (25.4500, 28.9900) },
            { Governorate.NorthSinai, (31.1218, 33.8070) },
            { Governorate.PortSaid, (31.2653, 32.3019) },
            { Governorate.Qalyubia, (30.3358, 31.2056) },
            { Governorate.Qena, (26.1551, 32.7160) },
            { Governorate.RedSea, (27.2579, 33.8116) },
            { Governorate.Sharqia, (30.5965, 31.5041) },
            { Governorate.Sohag, (26.5569, 31.6948) },
            { Governorate.SouthSinai, (28.9695, 33.6161) },
            { Governorate.Suez, (29.9668, 32.5498) }
        };

        /// <summary>
        /// Detect governorate from coordinates using nearest center
        /// Note: This is a simple approximation. For production, use proper boundary polygons
        /// or the Nominatim API response
        /// </summary>
        public static Governorate? DetectGovernorateFromCoordinates(double latitude, double longitude)
        {
            if (!IsValidEgyptianCoordinate(latitude, longitude))
                return null;

            // Find nearest governorate center
            double minDistance = double.MaxValue;
            Governorate? nearestGovernorate = null;

            foreach (var kvp in GovernorateCoordinates)
            {
                double distance = CalculateDistance(latitude, longitude, kvp.Value.Lat, kvp.Value.Lon);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestGovernorate = kvp.Key;
                }
            }

            return nearestGovernorate;
        }

        /// <summary>
        /// Parse governorate from Nominatim API address response
        /// This is the RECOMMENDED approach - more accurate than coordinate proximity
        /// </summary>
        public static Governorate? ParseGovernorateFromNominatimAddress(string? addressState)
        {
            if (string.IsNullOrWhiteSpace(addressState))
                return null;

            var normalized = addressState.Trim().ToLower()
                .Replace("محافظة", "") // Remove "governorate" in Arabic
                .Replace("governorate", "")
                .Trim();

            // Map common variations to enum
            var governorateMap = new Dictionary<string, Governorate>(StringComparer.OrdinalIgnoreCase)
            {
                // English names
                { "alexandria", Governorate.Alexandria },
                { "aswan", Governorate.Aswan },
                { "asyut", Governorate.Asyut },
                { "beheira", Governorate.Beheira },
                { "beni suef", Governorate.BeniSuef },
                { "bani suwayf", Governorate.BeniSuef },
                { "cairo", Governorate.Cairo },
                { "dakahlia", Governorate.Dakahlia },
                { "damietta", Governorate.Damietta },
                { "faiyum", Governorate.Faiyum },
                { "fayoum", Governorate.Faiyum },
                { "gharbia", Governorate.Gharbia },
                { "giza", Governorate.Giza },
                { "ismailia", Governorate.Ismailia },
                { "kafr el sheikh", Governorate.KafrElSheikh },
                { "kafr el-sheikh", Governorate.KafrElSheikh },
                { "kafr ash shaykh", Governorate.KafrElSheikh },
                { "luxor", Governorate.Luxor },
                { "matrouh", Governorate.Matrouh },
                { "matruh", Governorate.Matrouh },
                { "minya", Governorate.Minya },
                { "monufia", Governorate.Monufia },
                { "menofia", Governorate.Monufia },
                { "new valley", Governorate.NewValley },
                { "wadi el gedid", Governorate.NewValley },
                { "north sinai", Governorate.NorthSinai },
                { "port said", Governorate.PortSaid },
                { "qalyubia", Governorate.Qalyubia },
                { "qena", Governorate.Qena },
                { "red sea", Governorate.RedSea },
                { "sharqia", Governorate.Sharqia },
                { "sharqiyah", Governorate.Sharqia },
                { "sohag", Governorate.Sohag },
                { "south sinai", Governorate.SouthSinai },
                { "suez", Governorate.Suez },

                // Arabic names (transliterated)
                { "الإسكندرية", Governorate.Alexandria },
                { "أسوان", Governorate.Aswan },
                { "أسيوط", Governorate.Asyut },
                { "البحيرة", Governorate.Beheira },
                { "بني سويف", Governorate.BeniSuef },
                { "القاهرة", Governorate.Cairo },
                { "الدقهلية", Governorate.Dakahlia },
                { "دمياط", Governorate.Damietta },
                { "الفيوم", Governorate.Faiyum },
                { "الغربية", Governorate.Gharbia },
                { "الجيزة", Governorate.Giza },
                { "الإسماعيلية", Governorate.Ismailia },
                { "كفر الشيخ", Governorate.KafrElSheikh },
                { "الأقصر", Governorate.Luxor },
                { "مطروح", Governorate.Matrouh },
                { "المنيا", Governorate.Minya },
                { "المنوفية", Governorate.Monufia },
                { "الوادي الجديد", Governorate.NewValley },
                { "شمال سيناء", Governorate.NorthSinai },
                { "بورسعيد", Governorate.PortSaid },
                { "القليوبية", Governorate.Qalyubia },
                { "قنا", Governorate.Qena },
                { "البحر الأحمر", Governorate.RedSea },
                { "الشرقية", Governorate.Sharqia },
                { "سوهاج", Governorate.Sohag },
                { "جنوب سيناء", Governorate.SouthSinai },
                { "السويس", Governorate.Suez }
            };

            return governorateMap.TryGetValue(normalized, out var governorate) ? governorate : null;
        }

        /// <summary>
        /// Check if coordinates are within Egypt's approximate boundaries
        /// </summary>
        private static bool IsValidEgyptianCoordinate(double latitude, double longitude)
        {
            // Egypt approximate boundaries
            // Latitude: 22°N to 32°N
            // Longitude: 25°E to 35°E
            return latitude >= 22 && latitude <= 32 &&
                   longitude >= 25 && longitude <= 35;
        }

        /// <summary>
        /// Calculate distance between two coordinates (Haversine formula)
        /// </summary>
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        /// <summary>
        /// Get display name for governorate (with Arabic support)
        /// </summary>
        public static string GetDisplayName(Governorate governorate, bool includeArabic = false)
        {
            var attr = typeof(Governorate)
                .GetField(governorate.ToString())
                ?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;

            if (attr == null)
                return governorate.ToString();

            if (includeArabic && !string.IsNullOrEmpty(attr.ShortName))
                return $"{attr.Name} ({attr.ShortName})";

            return attr.Name ?? governorate.ToString();
        }

        /// <summary>
        /// Get all governorates for dropdown
        /// </summary>
        public static Dictionary<int, string> GetGovernorateList(bool includeArabic = false)
        {
            return Enum.GetValues<Governorate>()
                .OrderBy(g => GetDisplayName(g))
                .ToDictionary(g => (int)g, g => GetDisplayName(g, includeArabic));
        }
    }
}