using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a command button.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Submits the command.
        /// </summary>
        /// <returns>A task.</returns>
         Task <IReplyPacket>Execute();

        /// <summary>
        /// Returns true if the command has been executed; false as long as it still  executes.
        /// </summary>
        /// <returns>True when it's finished.</returns>
         Task<bool> IsDone();
    }
}