using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GigeVision.Core.Enums
{
    /// <summary>
    /// Camera Registrer Visibility
    /// </summary>
    public enum CameraRegisterVisibility
    {
        /// <summary>
        /// Beginner (First Level)
        /// </summary>
        Beginner,

        /// <summary>
        /// Expert(Second Level)
        /// </summary>
        Expert,

        /// <summary>
        /// Guru (Third Level)
        /// </summary>
        Guru,

        /// <summary>
        /// this level ment to be hidden
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Invisible,
    }
}