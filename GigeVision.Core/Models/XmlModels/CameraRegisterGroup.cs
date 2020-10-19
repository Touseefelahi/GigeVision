using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Register Group
    /// </summary>
    public class CameraRegisterGroup
    {
        /// <summary>
        /// Group Comment
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Group Category List that have all the features parameters of the group
        /// </summary>
        public List<string> Category { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="cameraRegisters"></param>
        public CameraRegisterGroup(string comment, List<string> cameraRegisters)
        {
            Comment = comment;
            Category = cameraRegisters;
        }
    }
}