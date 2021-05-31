using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace GenICam
{
    public static class TempDictionary
    {
        static TempDictionary()
        {
            Formula = new Dictionary<string, object>();
        }

        private static Dictionary<string, object> Formula { get; set; }

        public async static Task<bool> Add(string key, object value)
        {
            if (!Formula.ContainsKey(key))
            {
                Formula.Add(key, value);
                return true;
            }
            return false;
        }

        public async static Task<object> Get(string key)
        {
            if (Formula.ContainsKey(key))
                return Formula[key];

            return null;
        }
        public static void Clear()
        {
            Formula.Clear();
        }
    }
}