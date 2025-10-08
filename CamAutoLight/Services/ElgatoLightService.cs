using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CamAutoLight.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAutoLight.Services;

public class ElgatoLightService(IConfigManager configManager, ILogger<ElgatoLightService> logger)
    : IElgatoLightService
{
    private readonly List<string> _ipAddresses = configManager.IpAddresses;
    private readonly int _brightness = configManager.Brightness;
    private readonly int _temperature = configManager.Temperature;
    private static readonly HttpClient client = new();

    public void TurnOnLights()
    {
        foreach (var ip in _ipAddresses)
        {
            SendLightRequest(ip, _brightness, _temperature, true);
        }
    }

    public void TurnOffLights()
    {
        foreach (var ip in _ipAddresses)
        {
            SendLightRequest(ip, 0, 0, false);
        }
    }

    public void ToggleLights()
    {
        // Check the first light's state to determine what to do
        if (_ipAddresses.Count > 0)
        {
            bool isOn = GetLightState(_ipAddresses[0]);
            if (isOn)
            {
                logger.LogInformation("Lights are ON, turning OFF");
                TurnOffLights();
            }
            else
            {
                logger.LogInformation("Lights are OFF, turning ON");
                TurnOnLights();
            }
        }
    }

    private bool GetLightState(string ip)
    {
        string url = $"http://{ip}:9123/elgato/lights";

        try
        {
            var response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = response.Content.ReadAsStringAsync().Result;
                var jsonDoc = JsonDocument.Parse(jsonResponse);

                if (
                    jsonDoc.RootElement.TryGetProperty("lights", out var lightsArray)
                    && lightsArray.GetArrayLength() > 0
                )
                {
                    var firstLight = lightsArray[0];
                    if (firstLight.TryGetProperty("on", out var onProperty))
                    {
                        return onProperty.GetInt32() == 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ERROR] Failed to get state from {ip}", ip);
        }

        // Default to OFF if we can't determine state
        return false;
    }

    private void SendLightRequest(string ip, int brightness, int temperature, bool turnOn)
    {
        string json = turnOn
            ? $"{{\"lights\":[{{\"brightness\":{brightness},\"temperature\":{temperature},\"on\":1}}]}}"
            : "{\"lights\":[{\"on\":0}]}";

        StringContent content = new(json, Encoding.UTF8, "application/json");
        string url = $"http://{ip}:9123/elgato/lights";

        try
        {
            var response = client.PutAsync(url, content).Result;
            logger.LogInformation(
                "[LIGHT] {ip} -> {state} | Response: {code}",
                ip,
                turnOn ? "ON" : "OFF",
                response.StatusCode
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ERROR] Failed to send request to {ip}", ip);
        }
    }
}
