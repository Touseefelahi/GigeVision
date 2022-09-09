using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a string.
    /// </summary>
    public interface ISelector
    {
        /// <summary>
        /// Indicates if that node is a selector.
        /// </summary>
        /// <returns>True if it is a selector.</returns>
        public Task<bool> IsSelectorAsync();

        /// <summary>
        /// Returns a list of pointers to the feature nodes which are selected by the current node.
        /// </summary>
        /// <returns>The list of pointes on features.</returns>
        public Task<IEnumeration> GetSelectedEntryAsync();

        /// <summary>
        ///  Returns a list of pointers to the feature nodes which are selecting the the current node.
        /// </summary>
        /// <returns>The list of pointers on the features.</returns>
        public Task<IEnumeration> GetSelectingFeaturesAsync();
    }
}