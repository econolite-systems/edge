// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Econolite.Ode.Domain.Detector;
using Econolite.Ode.Models.VehiclePriority;
using Econolite.Ode.Models.VehiclePriority.Config;
using Microsoft.Extensions.Options;

namespace Edge.TestServer;

public class DetectorCountEdgeIngesterWorker: BackgroundService
{
    private readonly IOptionsMonitor<DetectorCountUdpOptions> _options;
    private readonly IOptionsMonitor<BsmUdpOptions> _optionsPublish;
    private readonly ILogger<DetectorCountEdgeIngesterWorker> _logger;

    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters ={
            new JsonStringEnumConverter()
        },
    };
    
    public DetectorCountEdgeIngesterWorker(IOptionsMonitor<DetectorCountUdpOptions> options, IOptionsMonitor<BsmUdpOptions> optionsPublish, ILogger<DetectorCountEdgeIngesterWorker> logger)
    {
        _options = options;
        _optionsPublish = optionsPublish;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var udpClient = new UdpClient(_options.CurrentValue.Port);
            while (true)
            {
                await Process(await udpClient.ReceiveAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

#pragma warning disable CS1998
    private async Task Process(UdpReceiveResult result)
#pragma warning restore CS1998
    {
        var json = Encoding.ASCII.GetString(result.Buffer);
        _logger.LogInformation(json);
        var request = JsonSerializer.Deserialize<DetectorCount>(json, _jsonOptions);
    }
}