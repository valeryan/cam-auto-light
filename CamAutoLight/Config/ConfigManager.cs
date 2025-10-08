using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CamAutoLight.Interfaces;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Zeroconf;

namespace CamAutoLight.Config;

public class ConfigManager(ILogger<ConfigManager> logger) : IConfigManager
{
    public List<string> IpAddresses { get; private set; } = [];
    public int Brightness { get; private set; }
    public int Temperature { get; private set; }

    public void LoadConfig()
    {
        // Load .env from the same directory as the executable
        var executableDir = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );
        var envPath = System.IO.Path.Combine(executableDir!, ".env");

        if (System.IO.File.Exists(envPath))
        {
            Env.Load(envPath);
            logger.LogInformation("Loaded .env from: {path}", envPath);
        }
        else
        {
            // Fallback to traversing from current directory
            Env.TraversePath().Load();
        }

        Brightness = Env.GetInt("brightness", -1);
        Temperature = Env.GetInt("temperature", -1);

        // Try ZeroConf discovery first
        var discovered = DiscoverElgatoLightsAsync().GetAwaiter().GetResult();
        if (discovered.Count > 0)
        {
            IpAddresses.AddRange(discovered);
            logger.LogInformation(
                "Discovered Elgato lights via ZeroConf: {ips}",
                string.Join(", ", discovered)
            );
        }
        else
        {
            string? ipList = Env.GetString("ip_addresses");
            if (!string.IsNullOrWhiteSpace(ipList))
            {
                IpAddresses.AddRange(ipList.Split(','));
                logger.LogInformation("Loaded Elgato light IPs from .env: {ips}", ipList);
            }
        }
    }

    private async Task<List<string>> DiscoverElgatoLightsAsync()
    {
        var results = await ZeroconfResolver.ResolveAsync("_elg._tcp.local.");
        var ips = new List<string>();
        foreach (var r in results)
        {
            if (r.IPAddress != null)
                ips.Add(r.IPAddress);
        }
        return ips;
    }

    public void ValidateConfig()
    {
        if (IpAddresses.Count == 0)
        {
            logger.LogError("Error: No IP addresses found via ZeroConf or .env.");
            throw new InvalidOperationException("No IP addresses found via ZeroConf or .env.");
        }

        if (Brightness < 1 || Brightness > 100)
        {
            logger.LogError("Error: Invalid brightness value in .env.");
            throw new InvalidOperationException("Invalid brightness value in .env.");
        }

        if (Temperature < 143 || Temperature > 344)
        {
            logger.LogError("Error: Invalid temperature value in .env.");
            throw new InvalidOperationException("Invalid temperature value in .env.");
        }
    }
}
