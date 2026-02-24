#if UNITY_EDITOR

using System;
using System.Globalization;

namespace NekoSerializer
{
    public partial class DataSerializationViewer
    {
        private static bool IsUtcDateTimeKey(string key) =>
            string.Equals(key, "LastSaveTime", StringComparison.OrdinalIgnoreCase);

        private static string FormatDateTimeSummary(DateTime dt, bool forceUtc)
        {
            // Requested style: "12/12/2025 05:35:35am"
            if (forceUtc)
                dt = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();

            return dt.ToString("MM/dd/yyyy hh:mm:sstt", CultureInfo.InvariantCulture)
                .ToLowerInvariant();
        }
    }
}

#endif
