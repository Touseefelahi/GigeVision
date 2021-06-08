using System;
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
        private static object lockObj = new Object();

        public async static Task<bool> Add(string key, object value)
        {
            lock (lockObj)
            {
                if (!Formula.ContainsKey(key))
                {
                    Formula.Add(key, value);
                    return true;
                }
                return false;
            }
        }

        public async static Task<object> Get(string key)
        {
            lock (lockObj)
            {
                if (Formula.ContainsKey(key))
                    return Formula[key];

                return null;
            }
        }
        public static void Clear()
        {
            lock (lockObj)
            {
                Formula.Clear();
            }
        }
    }
}