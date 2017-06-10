using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Util
{
    class DictUtils
    {
        public static IReadOnlyDictionary<TKey, TValue> FromEnumerable<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> arr)
        {
            var dict = new Dictionary<TKey, TValue>(arr.Count());
            foreach(var kvp in arr)
            {
                dict.Add(kvp.Key, kvp.Value);
            }
            return new ReadOnlyDictionary<TKey, TValue>(dict);
        }
    }
}
