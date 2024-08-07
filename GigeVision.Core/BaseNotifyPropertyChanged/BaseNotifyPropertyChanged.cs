﻿using System.ComponentModel;

namespace GigeVision.Core
{
    public abstract class BaseNotifyPropertyChanged : INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes its value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (_, e) => { };

        #region Public Methods

        /// <summary> Call this to fire a <see cref=”PropertyChanged”/> event </summary> <param name=”name”></param>
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion Public Methods
    }
}