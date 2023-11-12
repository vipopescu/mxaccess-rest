using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Identity.Client.Extensibility;

namespace MXAccesRestAPI.Global
{
    public static class GlobalConstants
    {
        private static IReadOnlyDictionary<int, string> OPCQualityDescription { get; }

        static GlobalConstants()
        {
            var dictionary = new Dictionary<int, string>
            {
                {0, "Bad [Non-Specific]"},
                {4, "Bad [Configuration Error]"},
                {8, "Bad [Not Connected]"},
                {12, "Bad [Device Failure]"},
                {16, "Bad [Sensor Failure]"},
                {20, "Bad [Last Known Value]"},
                {24, "Bad [Communication Failure]"},
                {28, "Bad [Out of Service]"},
                {32, "Bad [Waiting for initial data]"},
                {64, "Uncertain [Non-Specific]"},
                {65, "Uncertain [Non-Specific] (Low Limited)"},
                {66, "Uncertain [Non-Specific] (High Limited)"},
                {67, "Uncertain [Non-Specific] (Constant)"},
                {68, "Uncertain [Last Usable]"},
                {69, "Uncertain [Last Usable] (Low Limited)"},
                {70, "Uncertain [Last Usable] (High Limited)"},
                {71, "Uncertain [Last Usable] (Constant)"},
                {80, "Uncertain [Sensor Not Accurate]"},
                {81, "Uncertain [Sensor Not Accurate] (Low Limited)"},
                {82, "Uncertain [Sensor Not Accurate] (High Limited)"},
                {83, "Uncertain [Sensor Not Accurate] (Constant)"},
                {84, "Uncertain [EU Exceeded]"},
                {85, "Uncertain [EU Exceeded] (Low Limited)"},
                {86, "Uncertain [EU Exceeded] (High Limited)"},
                {87, "Uncertain [EU Exceeded] (Constant)"},
                {88, "Uncertain [Sub-Normal]"},
                {89, "Uncertain [Sub-Normal] (Low Limited)"},
                {90, "Uncertain [Sub-Normal] (High Limited)"},
                {91, "Uncertain [Sub-Normal] (Constant)"},
                {192, "Good [Non-Specific]"},
                {193, "Good [Non-Specific] (Low Limited)"},
                {194, "Good [Non-Specific] (High Limited)"},
                {195, "Good [Non-Specific] (Constant)"},
                {216, "Good [Local Override]"},
                {217, "Good [Local Override] (Low Limited)"},
                {218, "Good [Local Override] (High Limited)"},
                {219, "Good [Local Override] (Constant)"},
                {65536, "Bad [Non-Specific]"},
                {65540, "Bad [Configuration Error]"},
                {65544, "Bad [Not Connected]"},
                {65548, "Bad [Device Failure]"},
                {65552, "Bad [Sensor Failure]"},
                {65556, "Bad [Last Known Value]"},
                {65560, "Bad [Communication Failure]"},
                {65564, "Bad [Out of Service]"},
                {65600, "Uncertain [Non-Specific]"},
                {65601, "Uncertain [Non-Specific] (Low Limited)"},
                {65602, "Uncertain [Non-Specific] (High Limited)"}
            };
            OPCQualityDescription = new ReadOnlyDictionary<int, string>(dictionary);
        }

        public static string GetQualityDescription(int qualityCode)
        {
            string qualityDescr = "";
            if (OPCQualityDescription.ContainsKey(qualityCode)) qualityDescr = OPCQualityDescription[qualityCode];
            return qualityDescr;
        }

        public static bool IsGood(int qualityCode)
        {
            var goodCodes = new HashSet<int> { 192, 193, 194, 195, 216, 217, 218, 219 };
            return goodCodes.Contains(qualityCode);
        }
    }
}