using System.Collections.Generic;

namespace GenICam
{
    public static class TempDictionary
    {
        static TempDictionary()
        {
            Formula = new Dictionary<string, object>();
        }

        public static Dictionary<string, object> Formula { get; set; }

        public static void Clear()
        {
            Formula.Clear();
        }
    }
}