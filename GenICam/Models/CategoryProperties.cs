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
        /// <param name="rootName"></param>
        /// <param name="name"></param>
        /// <param name="toolTip"></param>
        /// <param name="description"></param>
        /// <param name="visibilty"></param>
        /// <param name="isStreamable"></param>
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