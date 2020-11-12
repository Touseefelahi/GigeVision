namespace GenICam
{
    public class CategoryProperties
    {
        public string RootName { get; set; }

        /// <summary>
        /// Category Name
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Category Description
        /// </summary>
        public string Description { get; internal set; }

        public string ToolTip { get; internal set; }

        /// <summary>
        /// Category Visibility Level
        /// </summary>
        public GenVisibility Visibility { get; internal set; }

        /// <summary>
        /// Indicates whether the Category is stream-able or not
        /// </summary>
        public bool IsStreamable { get; internal set; }

        public CategoryProperties(string rootName, string name, string toolTip, string description, GenVisibility visibilty, bool isStreamable)
        {
            RootName = rootName;
            Name = name;
            ToolTip = toolTip;
            Description = description;
            Visibility = visibilty;
            IsStreamable = isStreamable;
        }
    }
}