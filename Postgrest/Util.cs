using System;
using System.Reflection;

namespace Postgrest
{
    public static class Util
    {
        public static string GetAssemblyVersion()
        {
            var assembly = typeof(Postgrest.Client).Assembly;
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var name = assembly.GetName().Name;

            return $"{name.ToString().ToLower()}-csharp/{informationVersion}";
        }
    }

}
