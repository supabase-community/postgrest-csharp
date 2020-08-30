using System;
namespace Postgrest.Extensions
{
    public static class RangeExtensions
    {
        public static string ToPostgresString(this Range range) => $"[{range.Start},{range.End}]";
    }
}
