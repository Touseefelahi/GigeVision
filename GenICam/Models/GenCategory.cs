using System.Collections.Generic;
using Prism.Mvvm;

namespace GenICam
{
    /// <summary>
    /// GenICam Category implementation.
    /// </summary>
    public class GenCategory : BindableBase, ICategory
    {
        /// <inheritdoc/>
        public CategoryProperties CategoryProperties { get; internal set; }

        /// <inheritdoc/>
        public List<ICategory> PFeatures { get; set; }

        /// <inheritdoc/>
        public IPValue PValue { get; internal set; }

        /// <summary>
        /// Gets the group name.
        /// </summary>
        public string GroupName { get; internal set; }

        /// <summary>
        /// Gets the set value command.
        /// </summary>
        public System.Windows.Input.ICommand SetValueCommand { get; internal set; }

        /// <summary>
        /// Gets the get value command.
        /// </summary>
        public System.Windows.Input.ICommand GetValueCommand { get; internal set; }

        /// <inheritdoc/>
        public GenAccessMode AccessMode { get; set; }

        /// <summary>
        /// Gets the list of features.
        /// </summary>
        /// <returns>The list of features.</returns>
        public List<ICategory> GetFeatures() => PFeatures;
    }
}