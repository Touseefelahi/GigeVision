namespace GenICam
{
    /// <summary>
    /// Interface for a node.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Gets the access mode.
        /// </summary>
        public GenAccessMode AccessMode { get; }
    }
}