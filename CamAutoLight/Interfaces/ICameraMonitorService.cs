using System;

namespace CamAutoLight.Interfaces
{
    public interface ICameraMonitorService : IDisposable
    {
        void CheckInitialCameraState();
        void MonitorLogStream();
    }
}
