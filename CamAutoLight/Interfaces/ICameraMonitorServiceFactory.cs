using System.Runtime.InteropServices;

namespace CamAutoLight.Interfaces
{
    public interface ICameraMonitorServiceFactory
    {
        ICameraMonitorService CreateCameraMonitorService();
    }
}
