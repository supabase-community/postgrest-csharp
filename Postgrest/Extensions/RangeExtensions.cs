using System;
namespace Postgrest.Extensions
{

    public static class RangeExtensions
    {
        /// <summary>
        /// Transforms a C# Range to a Postgrest String.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public static string ToPostgresString(this Range range) => $"[{range.Start},{range.End}]";
    }
}
