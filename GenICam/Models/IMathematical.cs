using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IMathematical : IPValue
    {
        public double Value { get; }
    }
}