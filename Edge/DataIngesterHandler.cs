// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Econolite.Ode.Domain.Detector;
using Econolite.Ode.Domain.VehiclePriority;
using Econolite.Ode.Models.VehiclePriority.Config;
using Microsoft.Extensions.Options;

namespace Edge;

public class DataIngesterHandler : BackgroundService
{
    private readonly IOptionsMonitor<BsmUdpOptions> _options;
    private readonly VehicleBsmHandler _vehicleBsmHandler;
    private readonly VehiclePriorityIngesterHandler _vehiclePriorityIngesterHandler;
    private readonly ILogger<DataIngesterHandler> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters ={
            new JsonStringEnumConverter()
        },
    };
    
    public DataIngesterHandler(IOptionsMonitor<BsmUdpOptions> options, VehicleBsmHandler vehicleBsmHandler, VehiclePriorityIngesterHandler vehiclePriorityIngesterHandler, ILogger<DataIngesterHandler> logger)
    {
        _options = options;
        _vehicleBsmHandler = vehicleBsmHandler;
        _vehiclePriorityIngesterHandler = vehiclePriorityIngesterHandler;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                using var udpClient = new UdpClient(_options.CurrentValue.Port);
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessAsync(await udpClient.ReceiveAsync(stoppingToken));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception while processing message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Main processing loop terminated!");
            }
        });
    }

    private async Task ProcessAsync(UdpReceiveResult receiveAsync)
    {
        await Task.WhenAll(
            _vehiclePriorityIngesterHandler.ProcessAsync(receiveAsync),
            _vehicleBsmHandler.ProcessAsync(receiveAsync)
        );
    }
}