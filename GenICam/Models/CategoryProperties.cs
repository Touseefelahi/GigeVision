namespace GenICam
{
    /// <summary>
    /// Category properties class.
    /// </summary>
    public class CategoryProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryProperties"/> class.
        /// </summary>
        /// <param name="rootName">The root name.</param>
        /// <param name="name">The name.</param>
        /// <param name="toolTip">The tool tip.</param>
        /// <param name="description">The description.</param>
        /// <param name="visibilty">The visibility.</param>
        /// <param name="isStreamable">True if streamable.</param>
        public CategoryProperties(string rootName, string name, string toolTip, string description, GenVisibility visibilty, bool isStreamable)
        {
            RootName = rootName;
            Name = name;
            ToolTip = toolTip;
            Description = description;
            Visibility = visibilty;
            IsStreamable = isStreamable;
        }

        /// <summary>
        /// Gets or sets the root name.
        /// </summary>
        public string RootName { get; set; }

        /// <summary>
        /// Gets category Name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets category Description.
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// Gets the default value is an empty string.
        /// </summary>
        public string ToolTip { get; internal set; }

        /// <summary>
        /// Gets category Visibility Level.
        /// </summary>
        public GenVisibility Visibility { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether indicates whether the Category is stream-able or not.
        /// </summary>
        public bool IsStreamable { get; internal set; }
    }
}