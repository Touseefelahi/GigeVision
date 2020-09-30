using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// the main class that contains all register types
    /// </summary>
    public class CameraRegisterContainer
    {
        /// <summary>
        /// Register Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Register Description
        /// </summary>
        public string Descrption { get; private set; }

        /// <summary>
        /// Register Visibility Level
        /// </summary>
        public CameraRegisterVisibility? Visibility { get; private set; }

        /// <summary>
        /// Indicates whether the register is streamable or not
        /// </summary>
        public bool IsStreamable { get; private set; }

        /// <summary>
        /// Register Type
        /// </summary>
        public CameraRegisterType Type { get; private set; }

        /// <summary>
        /// Register Type Value
        /// </summary>
        public object TypeValue { get; set; }

        /// <summary>
        /// The Register Information of the Register Type
        /// </summary>
        public CameraRegister Register
        {
            get
            {
                if (TypeValue is CameraRegister cameraRegister)
                    return cameraRegister;

                if (TypeValue is IntegerRegister integerRegister)
                    return integerRegister.Register;

                if (TypeValue is Enumeration enumeration)
                    return enumeration.Register;

                if (TypeValue is CommandRegister commandRegister)
                    return commandRegister.Register;

                if (TypeValue is MaskedIntReg maskedIntReg)
                    return maskedIntReg.Register;

                if (TypeValue is BooleanRegister booleanRegister)
                    return booleanRegister.Register;

                return null;
            }
        }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="registerName"></param>
        /// <param name="description"></param>
        /// <param name="registerVisibility"></param>
        /// <param name="isStreamable"></param>
        /// <param name="registerType"></param>
        /// <param name="typeValue"></param>
        public CameraRegisterContainer(string registerName, string description, CameraRegisterVisibility? registerVisibility, bool isStreamable, CameraRegisterType registerType,
             object typeValue = null)
        {
            Name = registerName;
            Descrption = description;
            Visibility = registerVisibility;
            IsStreamable = isStreamable;
            Type = registerType;
            TypeValue = typeValue;
        }
    }
}