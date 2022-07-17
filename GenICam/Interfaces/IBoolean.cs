using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a check box
    /// </summary>
    public interface IBoolean 
    {
        public Task<bool> GetValueAsync();
        public Task SetValueAsync(bool value);
    }
}