namespace Postgrest
{
    /// <summary>
    /// Options that can be passed to the Client configuration
    /// </summary>
    public class StatelessClientOptions : ClientOptions
    {
        public StatelessClientOptions(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }
    }
}
