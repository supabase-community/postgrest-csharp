using System;
using System.Collections.Generic;
using System.Text;

namespace Postgrest.Extensions
{
    public static class UriExtensions
    {
        public static string GetBaseUrl(this Uri uri) =>
            uri.GetLeftPart(UriPartial.Authority);
    }
}
