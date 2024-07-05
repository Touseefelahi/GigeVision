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
         CategoryProperties CategoryProperties { get; }

        /// <summary>
        /// Gets or sets the PFeatures.
        /// </summary>
         List<ICategory> PFeatures { get; set; }

        /// <summary>
        /// Gets the PValue.
        /// </summary>
         IPValue PValue { get; }
    }
}