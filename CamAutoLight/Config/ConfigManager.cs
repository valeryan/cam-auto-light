using System;
using System.Collections.Generic;
using CamAutoLight.Interfaces;
using DotNetEnv;
using Microsoft.Extensions.Logging;

namespace CamAutoLight.Config;

public class ConfigManager(ILogger<ConfigManager> logger) : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger = logger;
    public List<string> IpAddresses { get; private set; } = [];
    public int Brightness { get; private set; }
    public int Temperature { get; private set; }

    public void LoadConfig()
    {
        Env.TraversePath().Load();

        string? ipList = Env.GetString("ip_addresses");
        if (!string.IsNullOrWhiteSpace(ipList))
        {
            IpAddresses.AddRange(ipList.Split(','));
        }

        Brightness = Env.GetInt("brightness", -1);
        Temperature = Env.GetInt("temperature", -1);
    }

    public void ValidateConfig()
    {
        if (IpAddresses.Count == 0)
        {
            _logger.LogError("Error: No IP addresses found in .env file.");
            throw new InvalidOperationException("No IP addresses found in .env file.");
        }

        if (Brightness < 1 || Brightness > 100)
        {
            _logger.LogError("Error: Invalid brightness value in .env.");
            throw new InvalidOperationException("Invalid brightness value in .env.");
        }

        if (Temperature < 143 || Temperature > 344)
        {
            _logger.LogError("Error: Invalid temperature value in .env.");
            throw new InvalidOperationException("Invalid temperature value in .env.");
        }
    }
}
