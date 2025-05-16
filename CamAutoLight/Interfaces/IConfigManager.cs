using System.Collections.Generic;

namespace CamAutoLight.Interfaces
{
    public interface IConfigManager
    {
        List<string> IpAddresses { get; }
        int Brightness { get; }
        int Temperature { get; }
        void LoadConfig();
        void ValidateConfig();
    }
}
