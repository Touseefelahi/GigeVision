using GigeVision.Core.Models;
using System;
using System.Threading.Tasks;

namespace GigeVisionTestApp
{
    internal class Program
    {
        private static Gvcp gvcp = new();

        private static void Main(string[] args)
        {
            TaskRun();
        }

        private static async void TaskRun()
        {
            var devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);

            await Task.Delay(5000).ConfigureAwait(false);
        }
    }
}