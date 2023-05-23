using System;

namespace Postgrest.Extensions;

public static class UriExtensions
{
    public static string GetInstanceUrl(this Uri uri) =>
        uri.GetLeftPart(UriPartial.Authority) + uri.LocalPath;
}