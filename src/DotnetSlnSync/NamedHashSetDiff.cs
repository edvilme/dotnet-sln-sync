using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetSlnSync
{
    internal class NamedHashSetDiff
    {
        public static List<(string Included, string Excluded, T obj)> GetNamedDiff<T>(IEnumerable<T> aItems, IEnumerable<T> bItems, string aName = "a", string bName = "b", IEqualityComparer<T>? equalityComparer = null)
        {
            // Store in list
            List<(string Included, string Excluded, T obj)> result = new();

            // Create hash sets for both enumerators
            HashSet<T> aSet = new HashSet<T>(aItems, equalityComparer);
            HashSet<T> bSet = new HashSet<T>(bItems, equalityComparer);

            // Find items in a that are not in b
            foreach (var item in aSet)
            {
                if (!bSet.Contains(item, equalityComparer))
                {
                    result.Add((aName, bName, item));
                }
            }

            // Find items in b that are not in a
            foreach (var item in bSet)
            {
                if (!aSet.Contains(item, equalityComparer))
                {
                    result.Add((bName, aName, item));
                }
            }
            return result;
        }
    }
}
