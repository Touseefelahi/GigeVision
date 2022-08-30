using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to an edit box showing a string
    /// </summary>
    public interface ISelector
    {
        /// <summary>
        /// Indicates if that node is a selector
        /// </summary>
        /// <returns></returns>
        Task<bool> IsSelectorAsync();
        /// <summary>
        /// Returns a list of pointers to the feature nodes which are selected by the current node.
        /// </summary>
        /// <returns></returns>
        Task<IEnumeration> GetSelectedEntryAsync();
        /// <summary>
        ///  Returns a list of pointers to the feature nodes which are selecting the the current node.
        /// </summary>
        /// <returns></returns>
        Task<IEnumeration> GetSelectingFeaturesAsync();
    }
}