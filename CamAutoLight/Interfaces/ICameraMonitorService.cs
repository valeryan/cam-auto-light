namespace CamAutoLight.Interfaces
{
    public interface ICameraMonitorService
    {
        void CheckInitialCameraState();
        void MonitorLogStream();
    }
}
