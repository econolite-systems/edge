// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Econolite.Ode.Models.Entities;
using Econolite.Ode.Models.VehiclePriority;
using Econolite.Ode.Models.VehiclePriority.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Econolite.Ode.Domain.Detector;

public class VehicleBsmHandler
{
    private readonly VehicleCountService _vehicleCountService;
    private readonly IOptionsMonitor<DetectorCountUdpOptions> _options;
    private readonly ILogger<VehicleBsmHandler> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters ={
            new JsonStringEnumConverter()
        },
    };

    public VehicleBsmHandler(IOptionsMonitor<DetectorCountUdpOptions> options, VehicleCountService vehicleCountService, ILogger<VehicleBsmHandler> logger)
    {
        _options = options;
        _vehicleCountService = vehicleCountService;
        _logger = logger;
    }
    
    public async Task ProcessAsync(UdpReceiveResult result)
    {
        var json = Encoding.ASCII.GetString(result.Buffer);
        if (json.Contains("bsm"))
        {
            var bsmMessage = JsonSerializer.Deserialize<BsmMessage>(json, _jsonOptions);
            if (bsmMessage == null) return;
            await ProcessBsmAsync(bsmMessage);
        }
    }

    private async Task ProcessBsmAsync(BsmMessage bsmMessage)
    {
        foreach (var message in bsmMessage.BsmMessageContent)
        {
            var id = message.Bsm.CoreData.Id.ToString();
            var point = new GeoJsonPointFeature()
            {
                Coordinates = ToCoordinate(message.Bsm.CoreData.Long, message.Bsm.CoreData.Lat)
            };
            var (detectorId, count) = await _vehicleCountService.CountVehicle(id, point);
            if (count)
            {
                // Send out count
                await PublishCountAsync(detectorId);
            }
        }
    }
    
    private async Task PublishCountAsync(int detectorId)
    {
        using var udpClient = new UdpClient();
        var detectorCount = new DetectorCount(detectorId);
        var json = JsonSerializer.Serialize(detectorCount, _jsonOptions);
        var package = System.Text.Encoding.UTF8.GetBytes(json);
        var ip = new IPEndPoint(IPAddress.Parse(_options.CurrentValue.Host), _options.CurrentValue.Port);
        _logger.LogInformation("Sending detector count {@}", detectorId);
        await udpClient.SendAsync(package, package.Length, ip);
    }
    
    private double[] ToCoordinate(int lon, int lat)
    {
        return new double[] { ToDouble(lon), ToDouble(lat) };
    }
    
    private double ToDouble(int value)
    {
        return (double)(value) / 10000000;
    }
}

public record DetectorCount(int DetectorId) { }