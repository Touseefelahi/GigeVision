using System.Collections.Generic;

namespace GenICam
{
    /// <summary>
    /// Maps to an entry in a tree structuring the camera's features.
    /// </summary>
    public interface ICategory 
    {
        /// <summary>
        /// Gets the category properties.
        /// </summary>
        public CategoryProperties CategoryProperties { get; }

        /// <summary>
        /// Gets or sets the PFeatures.
        /// </summary>
        public List<ICategory> PFeatures { get; set; }

        /// <summary>
        /// Gets the PValue.
        /// </summary>
        public IPValue PValue { get; }
    }
}