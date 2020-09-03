using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using GigeVision.Wpf.DTO;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GigeVision.Wpf.ViewModels
{
    public class DeviceControlViewModel : BindableBase
    {
        public List<CameraRegisterGroup> CameraRegisterGroup { get; set; }
        public ICamera Camera { get; }

        public DeviceControlViewModel(ICamera camera)
        {
            Camera = camera;

            var group = Camera.Gvcp.RegistersDictionary.GroupBy(x => x.Value.Comment).ToList();

            CameraRegisterGroup = new List<CameraRegisterGroup>();

            foreach (var item in group)
            {
                CameraRegisterGroup.Add(new CameraRegisterGroup(item.Key, item.Select(x => x.Value).ToList()));
            }
        }
    }
}