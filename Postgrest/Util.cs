using System.Reflection;

namespace Postgrest
{
    public static class Util
    {
        public static string GetAssemblyVersion()
        {
            var assembly = typeof(Client).Assembly;
            
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            
            var name = assembly.GetName().Name;

            return $"{name.ToLower()}-csharp/{informationVersion}";
        }
    }
}