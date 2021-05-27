using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    public static class TempDictionary
    {
        public static Dictionary<string, object> Formula { get; set; }

        static TempDictionary()
        {
            Formula = new Dictionary<string, object>();
        }

        public static void Clear()
        {
            Formula.Clear();
        }
    }
}
