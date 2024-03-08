// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Econolite.Ode.Models.VehiclePriority;
using Econolite.Ode.Models.VehiclePriority.Config;
using Microsoft.Extensions.Options;

namespace Edge.TestServer;

public class VehiclePriorityEdgeRequestIngesterWorker: BackgroundService
{
    private readonly IOptionsMonitor<PriorityRequestUdpOptions> _options;
    private readonly IOptionsMonitor<BsmUdpOptions> _optionsPublish;
    private readonly ILogger<VehiclePriorityEdgeRequestIngesterWorker> _logger;

    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters ={
            new JsonStringEnumConverter()
        },
    };
    
    public VehiclePriorityEdgeRequestIngesterWorker(IOptionsMonitor<PriorityRequestUdpOptions> options, IOptionsMonitor<BsmUdpOptions> optionsPublish, ILogger<VehiclePriorityEdgeRequestIngesterWorker> logger)
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
        var request = JsonSerializer.Deserialize<PriorityRequestMessage>(json, _jsonOptions);
        if (request == null) return;
        var response = new PriorityResponseMessage(
            new PriorityResponse(
                2, 
                request.Request.RequestId,
                request.Request.StrategyNumber,
                request.Request.VehicleClassType,
                request.Request.VehicleClassLevel,
                request.Request.VehicleId));
        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
        var package = System.Text.Encoding.UTF8.GetBytes(responseJson);
        using var udpClient = new UdpClient();
        var ip = new IPEndPoint(IPAddress.Parse(_optionsPublish.CurrentValue.Host), _optionsPublish.CurrentValue.Port);
        await udpClient.SendAsync(package, package.Length, ip);
        var prs = request.RequestType == PriorityRequestType.Cancel
            ? PriorityRequestStatus.ActiveCancel
            : PriorityRequestStatus.ActiveProcessing;
        var status = new PriorityStatusMessage(new[]
        {
            new PriorityStatus((int) prs, request.Request.RequestId, request.Request.StrategyNumber,
                request.Request.VehicleClassType, request.Request.VehicleClassLevel, request.Request.VehicleId),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
            new PriorityStatus(0, 1, 10,
                10, 10, "INVALID-VEH-ID-##"),
        });
        
        var statusJson = JsonSerializer.Serialize(status, _jsonOptions);
        package = System.Text.Encoding.UTF8.GetBytes(statusJson);
        await udpClient.SendAsync(package, package.Length, ip);
    }
}