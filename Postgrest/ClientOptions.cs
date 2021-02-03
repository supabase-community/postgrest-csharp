using System;
using System.Collections.Generic;

namespace Postgrest
{
    /// <summary>
    /// Options that can be passed to the Client configuration
    /// </summary>
    public class ClientOptions
    {
        public string Schema { get; set; } = "public";
        public System.Globalization.DateTimeStyles DateTimeStyles = System.Globalization.DateTimeStyles.AdjustToUniversal;
        public string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFK";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
    }
}
