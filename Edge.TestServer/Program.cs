// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Domain.Detector;
using Econolite.Ode.Models.VehiclePriority.Config;
using Edge.TestServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context,services) =>
    {
        services.AddMemoryCache();
        services.Configure<BsmUdpOptions>(
            context.Configuration.GetSection(BsmUdpOptions.Section));
        services.AddHostedService<BsmPublisherWorker>();
        services.Configure<DetectorCountUdpOptions>(
            context.Configuration.GetSection(DetectorCountUdpOptions.Section));
        services.AddHostedService<DetectorCountEdgeIngesterWorker>();
        services.AddHostedService<VehiclePriorityEdgeRequestIngesterWorker>();
    })
    .Build();

await host.RunAsync();