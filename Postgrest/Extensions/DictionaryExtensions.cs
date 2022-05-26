using System.Collections.Generic;
using System.Linq;

namespace Postgrest.Extensions
{
    public static class DictionaryExtensions
    {
        // Works in C#3/VS2008:
        // Returns a new dictionary of this ... others merged leftward.
        // Keeps the type of 'this', which must be default-instantiable.
        // Example: 
        //   result = map.MergeLeft(other1, other2, ...)
        // From: https://stackoverflow.com/a/2679857/3629438
        public static T MergeLeft<T, TK, TV>(this T me, params IDictionary<TK, TV>[] others)
            where T : IDictionary<TK, TV>, new()
        {
            var map = new T();
            var dictionaries = new List<IDictionary<TK, TV>> {me}.Concat(others);

            foreach (var dictionary in dictionaries)
            {
                // ^-- echk. Not quite there type-system.
                foreach (var keyValuePair in dictionary)
                {
                    map[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return map;
        }
    }
}