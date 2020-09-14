using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace GigeVision.Wpf.DTO
{
    public class CameraRegisterGroupDTO
    {
        public ICamera Camera { get; }
        public string Name { get; private set; }
        public ObservableCollection<CameraRegisterGroupDTO> Child { get; set; }
        public CameraRegister CameraRegister { get; set; }
        public List<CameraRegisterDTO> CameraRegisters { get; set; }

        public CameraRegisterGroupDTO(ICamera camera, string name, ObservableCollection<CameraRegisterGroupDTO> child, CameraRegister cameraRegister = null, List<CameraRegisterDTO> cameraRegisters = null)
        {
            Camera = camera;
            Name = name;
            Child = child;
            CameraRegister = cameraRegister;
            CameraRegisters = cameraRegisters;
        }
    }
}