using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Helpers
{
    /// <summary>
    /// Fast, accurate governorate detection using bounding boxes and city points
    /// Based on official Egyptian governorate boundaries and major cities
    /// </summary>
    public static class GovernorateGeometry
    {
        /// <summary>
        /// Governorate bounding boxes (latitude/longitude rectangles)
        /// Format: MinLat, MaxLat, MinLon, MaxLon
        /// </summary>
        private static readonly Dictionary<Governorate, BoundingBox> GovernorateBounds = new()
        {
            // Greater Cairo Region
            { Governorate.Cairo, new BoundingBox(29.85, 30.20, 31.10, 31.60) },
            { Governorate.Giza, new BoundingBox(29.40, 30.20, 30.70, 31.40) },
            { Governorate.Qalyubia, new BoundingBox(30.05, 30.60, 31.00, 31.40) },

            // Nile Delta - Northern Governorates
            { Governorate.Alexandria, new BoundingBox(30.80, 31.35, 29.70, 30.20) },
            { Governorate.Beheira, new BoundingBox(30.20, 31.50, 29.90, 30.90) },
            { Governorate.KafrElSheikh, new BoundingBox(31.00, 31.50, 30.50, 31.20) },
            { Governorate.Damietta, new BoundingBox(31.20, 31.60, 31.60, 32.00) },
            { Governorate.PortSaid, new BoundingBox(31.15, 31.35, 32.20, 32.40) },
            { Governorate.Suez, new BoundingBox(29.80, 30.20, 32.40, 32.70) },
            
            // Nile Delta - Central Governorates  
            { Governorate.Gharbia, new BoundingBox(30.70, 31.20, 30.80, 31.30) },
            { Governorate.Monufia, new BoundingBox(30.30, 30.80, 30.70, 31.30) },
            { Governorate.Dakahlia, new BoundingBox(30.80, 31.50, 31.00, 32.00) }, // Mansoura is here
            { Governorate.Sharqia, new BoundingBox(30.20, 31.20, 31.20, 32.20) }, // East of Dakahlia
            { Governorate.Ismailia, new BoundingBox(30.30, 31.00, 32.00, 32.80) }, // East of Sharqia

            // Middle Egypt
            { Governorate.BeniSuef, new BoundingBox(28.70, 29.50, 30.70, 31.50) },
            { Governorate.Faiyum, new BoundingBox(28.90, 29.80, 30.20, 31.20) },
            { Governorate.Minya, new BoundingBox(27.70, 29.00, 30.40, 31.20) },

            // Upper Egypt
            { Governorate.Asyut, new BoundingBox(26.90, 27.70, 30.50, 31.50) },
            { Governorate.Sohag, new BoundingBox(26.20, 27.20, 31.30, 32.20) },
            { Governorate.Qena, new BoundingBox(25.70, 26.50, 32.40, 33.20) },
            { Governorate.Luxor, new BoundingBox(25.40, 25.90, 32.50, 32.90) },
            { Governorate.Aswan, new BoundingBox(22.00, 24.50, 31.80, 33.50) },

            // Coastal & Desert Governorates
            { Governorate.Matrouh, new BoundingBox(28.50, 31.80, 24.50, 28.50) },
            { Governorate.RedSea, new BoundingBox(22.00, 29.00, 32.50, 35.50) },
            { Governorate.NewValley, new BoundingBox(22.00, 29.00, 25.00, 31.00) },
            { Governorate.NorthSinai, new BoundingBox(30.00, 31.50, 32.50, 34.50) },
            { Governorate.SouthSinai, new BoundingBox(27.50, 30.50, 32.80, 34.90) }
        };

        /// <summary>
        /// Major cities and their governorates for disambiguation
        /// When coordinates fall in overlapping boundaries, check nearest city
        /// </summary>
        private static readonly List<CityPoint> MajorCities = new()
        {
            // Greater Cairo
            new CityPoint("Cairo", Governorate.Cairo, 30.0444, 31.2357),
            new CityPoint("Giza", Governorate.Giza, 30.0131, 31.2089),
            new CityPoint("6th of October", Governorate.Giza, 29.9333, 31.0167),
            new CityPoint("Shubra El Kheima", Governorate.Qalyubia, 30.1286, 31.2422),
            new CityPoint("Banha", Governorate.Qalyubia, 30.4592, 31.1844),

            // Alexandria & Northern Coast
            new CityPoint("Alexandria", Governorate.Alexandria, 31.2001, 29.9187),
            new CityPoint("Damanhour", Governorate.Beheira, 31.0341, 30.4681),
            new CityPoint("Kafr El Sheikh", Governorate.KafrElSheikh, 31.1107, 30.9388),
            new CityPoint("Damietta", Governorate.Damietta, 31.4175, 31.8144),
            new CityPoint("Port Said", Governorate.PortSaid, 31.2653, 32.3019),
            new CityPoint("Suez", Governorate.Suez, 29.9668, 32.5498),

            // Delta Central
            new CityPoint("Tanta", Governorate.Gharbia, 30.7865, 31.0004),
            new CityPoint("Shibin El Kom", Governorate.Monufia, 30.5522, 31.0092),
            new CityPoint("Mansoura", Governorate.Dakahlia, 31.0379, 31.3815), // ✅ CRITICAL
            new CityPoint("Zagazig", Governorate.Sharqia, 30.5876, 31.5029), // ✅ CRITICAL
            new CityPoint("Ismailia", Governorate.Ismailia, 30.5833, 32.2667), // ✅ CRITICAL
            new CityPoint("10th of Ramadan", Governorate.Sharqia, 30.3000, 31.7500),
            new CityPoint("Bilbeis", Governorate.Sharqia, 30.4200, 31.5600),

            // Middle Egypt
            new CityPoint("Beni Suef", Governorate.BeniSuef, 29.0661, 31.0994),
            new CityPoint("Faiyum", Governorate.Faiyum, 29.3084, 30.8428),
            new CityPoint("Minya", Governorate.Minya, 28.0871, 30.7618),

            // Upper Egypt
            new CityPoint("Asyut", Governorate.Asyut, 27.1809, 31.1837),
            new CityPoint("Sohag", Governorate.Sohag, 26.5569, 31.6948),
            new CityPoint("Qena", Governorate.Qena, 26.1551, 32.7160),
            new CityPoint("Luxor", Governorate.Luxor, 25.6872, 32.6396),
            new CityPoint("Aswan", Governorate.Aswan, 24.0889, 32.8998),

            // Special Zones
            new CityPoint("Marsa Matrouh", Governorate.Matrouh, 31.3543, 27.2373),
            new CityPoint("Hurghada", Governorate.RedSea, 27.2579, 33.8116),
            new CityPoint("Sharm El Sheikh", Governorate.SouthSinai, 27.9158, 34.3300),
            new CityPoint("Arish", Governorate.NorthSinai, 31.1218, 33.8070),
            new CityPoint("Kharga", Governorate.NewValley, 25.4500, 28.9900)
        };

        /// <summary>
        /// 🚀 FAST & ACCURATE: Detect governorate from coordinates
        /// Uses bounding boxes first (O(1)), then nearest city for disambiguation (O(n))
        /// </summary>
        public static Governorate? DetectGovernorate(double latitude, double longitude)
        {
            // Step 1: Check if coordinates are in Egypt
            if (!IsInEgypt(latitude, longitude))
                return null;

            // Step 2: Find all governorates whose bounding box contains this point
            var candidateGovernorates = GovernorateBounds
                .Where(kvp => kvp.Value.Contains(latitude, longitude))
                .Select(kvp => kvp.Key)
                .ToList();

            if (candidateGovernorates.Count == 0)
            {
                // Point not in any bounding box - find nearest city as fallback
                return FindNearestCity(latitude, longitude)?.Governorate;
            }

            if (candidateGovernorates.Count == 1)
            {
                // Unambiguous match
                return candidateGovernorates[0];
            }

            // Step 3: Multiple candidates (overlapping boundaries) - find nearest city
            var nearestCity = FindNearestCityInGovernorates(latitude, longitude, candidateGovernorates);
            return nearestCity?.Governorate ?? candidateGovernorates[0];
        }

        /// <summary>
        /// Validate if selected governorate matches the coordinates
        /// Returns true if coordinates are within governorate bounds
        /// </summary>
        public static bool ValidateGovernorate(Governorate governorate, double latitude, double longitude)
        {
            if (!IsInEgypt(latitude, longitude))
                return false;

            // Check if point is within governorate bounding box
            if (GovernorateBounds.TryGetValue(governorate, out var bounds))
            {
                if (bounds.Contains(latitude, longitude))
                    return true;

                // Allow slight tolerance for boundary edges (±0.1 degrees ≈ 11km)
                var expandedBounds = bounds.Expand(0.1);
                if (expandedBounds.Contains(latitude, longitude))
                {
                    // Double-check with nearest city
                    var detected = DetectGovernorate(latitude, longitude);
                    return detected == governorate;
                }
            }

            return false;
        }

        /// <summary>
        /// Get distance between governorate center and coordinates
        /// Useful for sorting results by proximity
        /// </summary>
        public static double GetDistanceToGovernorate(Governorate governorate, double latitude, double longitude)
        {
            if (!GovernorateBounds.TryGetValue(governorate, out var bounds))
                return double.MaxValue;

            // Calculate distance to governorate center
            var centerLat = (bounds.MinLat + bounds.MaxLat) / 2;
            var centerLon = (bounds.MinLon + bounds.MaxLon) / 2;

            return CalculateDistance(latitude, longitude, centerLat, centerLon);
        }

        #region Helper Methods

        private static bool IsInEgypt(double latitude, double longitude)
        {
            // Egypt boundaries: 22°N to 32°N, 25°E to 36°E
            return latitude >= 22.0 && latitude <= 32.0 &&
                   longitude >= 25.0 && longitude <= 36.0;
        }

        private static CityPoint? FindNearestCity(double latitude, double longitude)
        {
            double minDistance = double.MaxValue;
            CityPoint? nearest = null;

            foreach (var city in MajorCities)
            {
                double distance = CalculateDistance(latitude, longitude, city.Latitude, city.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = city;
                }
            }

            // Only return if within reasonable distance (< 50km)
            return minDistance < 50 ? nearest : null;
        }

        private static CityPoint? FindNearestCityInGovernorates(
            double latitude,
            double longitude,
            List<Governorate> candidateGovernorates)
        {
            double minDistance = double.MaxValue;
            CityPoint? nearest = null;

            var relevantCities = MajorCities
                .Where(c => candidateGovernorates.Contains(c.Governorate));

            foreach (var city in relevantCities)
            {
                double distance = CalculateDistance(latitude, longitude, city.Latitude, city.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = city;
                }
            }

            return nearest;
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for great-circle distance
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
            return degrees * Math.PI / 180;
        }

        #endregion

        #region Data Structures

        private class BoundingBox
        {
            public double MinLat { get; }
            public double MaxLat { get; }
            public double MinLon { get; }
            public double MaxLon { get; }

            public BoundingBox(double minLat, double maxLat, double minLon, double maxLon)
            {
                MinLat = minLat;
                MaxLat = maxLat;
                MinLon = minLon;
                MaxLon = maxLon;
            }

            public bool Contains(double lat, double lon)
            {
                return lat >= MinLat && lat <= MaxLat &&
                       lon >= MinLon && lon <= MaxLon;
            }

            public BoundingBox Expand(double margin)
            {
                return new BoundingBox(
                    MinLat - margin,
                    MaxLat + margin,
                    MinLon - margin,
                    MaxLon + margin
                );
            }
        }

        private class CityPoint
        {
            public string Name { get; }
            public Governorate Governorate { get; }
            public double Latitude { get; }
            public double Longitude { get; }

            public CityPoint(string name, Governorate governorate, double latitude, double longitude)
            {
                Name = name;
                Governorate = governorate;
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        #endregion
    }
}