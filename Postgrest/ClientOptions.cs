using System.Collections.Generic;
using System.Globalization;

namespace Postgrest
{
    /// <summary>
    /// Options that can be passed to the Client configuration
    /// </summary>
    public class ClientOptions
    {
        public string Schema { get; set; } = "public";

        public readonly DateTimeStyles DateTimeStyles = DateTimeStyles.AdjustToUniversal;

        public const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFK";

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
    }
}