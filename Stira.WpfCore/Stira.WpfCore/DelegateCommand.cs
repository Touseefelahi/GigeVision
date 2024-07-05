using System;
using System.Windows.Input;

namespace Stira.WpfCore
{
    public class DelegateCommand : ICommand
    {
        #region Private Members

        /// <summary>
        /// The action to run
        /// </summary>
        private readonly Action mAction;

        #endregion Private Members

        #region Public Events

        /// <summary>
        /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
        /// </summary>
        public event EventHandler CanExecuteChanged = (sender, e) => { };

        #endregion Public Events

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DelegateCommand(Action action)
        {
            mAction = action;
        }

        #endregion Constructor

        #region Command Methods

        /// <summary>
        /// A relay command can always execute
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Executes the commands Action
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            mAction();
        }

        #endregion Command Methods
    }
}