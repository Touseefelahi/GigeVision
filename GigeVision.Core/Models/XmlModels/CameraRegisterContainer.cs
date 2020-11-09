using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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
        /// The Register Information of the Register Type
        /// </summary>
        public CameraRegister Register { get; set; }

        /// <summary>
        /// Register Type
        /// </summary>
        public CameraRegisterType Type { get; private set; }

        /// <summary>
        /// Register Type Value
        /// </summary>
        public object TypeValue { get; set; }

        /// <summary>
        /// Register Value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="registerName"></param>
        /// <param name="description"></param>
        /// <param name="registerVisibility"></param>
        /// <param name="isStreamable"></param>
        /// <param name="registerType"></param>
        /// <param name="typeValue"></param>
        public CameraRegisterContainer(string registerName, string description, CameraRegisterVisibility? registerVisibility, bool isStreamable, CameraRegisterType cameraRegisterType, object typeValue = null, CameraRegister cameraRegister = null, object value = null)
        {
            Name = registerName;
            Descrption = description;
            Visibility = registerVisibility;
            IsStreamable = isStreamable;
            Register = cameraRegister;
            Value = value;
            Type = cameraRegisterType;
            TypeValue = typeValue;
            if (Value is null)
            {
                if (TypeValue is IntSwissKnife intSwiss)
                    Value = intSwiss.Value;

                if (TypeValue is IntegerRegister integerRegister)
                    Value = integerRegister.Value;

                if (TypeValue is Enumeration enumeration)
                    Value = enumeration.Value;
            }

            //Type = registerType;
            //if (typeValue is CameraRegister cameraRegister)
            //    Register = cameraRegister;
            //else
            //    TypeValue = typeValue;
        }
    }
}