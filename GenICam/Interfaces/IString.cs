using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a string
    /// </summary>
    public interface IString : IPValue<string>
    {
        Task<string> GetValueAsync();

        Task SetValueAsync(string value);
        /// <summary>
        /// Gets the maximum length of the string
        /// </summary>
        /// <returns></returns>
        long GetMaxLength();
    }
}