// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Econolite.Ode.Models.Entities;
using Econolite.Ode.Models.VehiclePriority.Config;
using Microsoft.Extensions.Options;

namespace Edge.TestServer;

public class BsmPublisherWorker: BackgroundService
{
    private readonly string _path = "13MileAtMoundRdHeadingNorth.json";
    private readonly IOptionsMonitor<BsmUdpOptions> _options;
    private readonly ILogger<BsmPublisherWorker> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters ={
            new JsonStringEnumConverter()
        },
    };
    
    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    public BsmPublisherWorker(IOptionsMonitor<BsmUdpOptions> options, ILogger<BsmPublisherWorker> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var pointPath = await LoadJsonAsync();
            var currentPoint = 0;
            using var udpClient = new UdpClient();
            while (true)
            {
                var point = pointPath.Coordinates[currentPoint];
                var lon = (int)(point[0] * 10000000);
                var lat = (int)(point[1] * 10000000);
                _logger.LogInformation("Current point: {CurrentPoint} lon: {Longitude} lat: {Latitude}", currentPoint, lon.ToString(), lat.ToString());
                await Task.Delay(1000);
                var json = "{\"BsmMessageContent\": [{\"metadata\": {\"utctimestamp\": \"2022-07-18T17:04:23.347198Z\"}, \"bsm\": {\"coreData\": {\"msgCnt\": 0, \"id\": 365779719, \"secMark\": 365779719, \"lat\": "+lat+", \"long\": "+lon+", \"elev\": 0, \"accuracy\": { \"semiMajor\": 0, \"semiMinor\": 0, \"orientation\": 0}, \"transmission\": 7, \"speed\": 558, \"heading\": 28635, \"angle\": 127, \"accelSet\": { \"long\": 0, \"lat\": 0, \"vert\": 0, \"yaw\": 0 }, \"brakes\": {\"wheelBrakes\": 0, \"traction\": 0, \"abs\": 0, \"scs\": 0, \"brakeBoost\": 0, \"auxBrakes\": 0  }, \"size\": { \"width\": 0, \"length\": 0 }}}}]}";
                if (currentPoint == pointPath.Coordinates.Length - 1)
                {
                    currentPoint = 0;
                }
                else
                {
                    currentPoint += 1;
                }
                var package = System.Text.Encoding.UTF8.GetBytes(json);
                var ip = new IPEndPoint(IPAddress.Parse(_options.CurrentValue.Host), _options.CurrentValue.Port);
                await udpClient.SendAsync(package, package.Length, ip);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
    
    public async Task<GeoJsonLineStringFeature> LoadJsonAsync()
    {
        GeoJsonLineStringFeature? result = null;
        using StreamReader r = new StreamReader(_path);
        var json = await r.ReadToEndAsync();
        result = JsonSerializer.Deserialize<GeoJsonLineStringFeature>(json, _jsonSerializerOptions);
        return result ?? new GeoJsonLineStringFeature();
    }
}