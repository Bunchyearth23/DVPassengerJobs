using System;
using System.Collections.Generic;
using System.Linq;

namespace PassengerJobs.Integration
{
    internal static class SaveIntegrityPolicy
    {
        public static bool HasMissingReference<T>(IEnumerable<T>? values) where T : class =>
            values == null || values.Any(value => value == null);

        public static T[] AppendDistinct<T>(IEnumerable<T>? existing, IEnumerable<T>? incoming, Func<T, string?> getId)
            where T : class
        {
            var result = new List<T>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in (existing ?? Enumerable.Empty<T>()).Concat(incoming ?? Enumerable.Empty<T>()))
            {
                if (item == null) continue;
                var id = getId(item);
                if (string.IsNullOrWhiteSpace(id)) result.Add(item);
                else if (ids.Add(id!)) result.Add(item);
            }

            return result.ToArray();
        }
    }
}
