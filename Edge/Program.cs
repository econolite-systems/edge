// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Common.Extensions;
using Econolite.Ode.Domain.Detector;
using Econolite.Ode.Domain.Detector.Extensions;
using Econolite.Ode.Domain.SystemModeller;
using Econolite.Ode.Domain.SystemModeller.Edge;
using Econolite.Ode.Domain.SystemModeller.Extensions;
using Econolite.Ode.Domain.VehiclePriority;
using Econolite.Ode.Domain.VehiclePriority.Extensions;
using Econolite.Ode.Messaging.Extensions;
using Econolite.Ode.Models.VehiclePriority.Config;
using Econolite.Ode.Monitoring.Events.Extensions;
using Econolite.Ode.Monitoring.Metrics.Extensions;
using Econolite.Ode.Repository.VehiclePriority;
using Econolite.OdeRepository.SystemModeller;
using Edge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


await Host.CreateDefaultBuilder(args)
    .AddODELogging()
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.Configure<WhitelistOptions>(
            context.Configuration.GetSection(WhitelistOptions.Section));
        services.Configure<BsmUdpOptions>(
            context.Configuration.GetSection(BsmUdpOptions.Section));
        services.Configure<DetectorCountUdpOptions>(
            context.Configuration.GetSection(DetectorCountUdpOptions.Section));
        services.Configure<PriorityRequestUdpOptions>(
            context.Configuration.GetSection(PriorityRequestUdpOptions.Section));
        services.Configure<IfmUdpOptions>(
            context.Configuration.GetSection(IfmUdpOptions.Section));
        services.AddMessaging();
        services.AddMetrics(context.Configuration, "Vehicle Priority Edge")
            .AddUserEventSupport(context.Configuration, _ =>
            {
                _.DefaultSource = "Edge Service";
                _.DefaultLogName = Econolite.Ode.Monitoring.Events.LogName.SystemEvent;
                _.DefaultCategory = Econolite.Ode.Monitoring.Events.Category.Server;
                _.DefaultTenantId = Guid.Empty;
            });
        services.AddIntersectionConfigEdgeWorker(options =>
                options.DefaultChannel = context.Configuration["Topics:ConfigIntersectionRequest"] ??
                                         throw new NullReferenceException(
                                             "Topic:ConfigIntersectionRequest missing in config"),
            options => options.DefaultChannel = context.Configuration["Topics:ConfigIntersectionResponse"] ??
                                                throw new NullReferenceException(
                                                    "Topic:ConfigIntersectionResponse missing in config"));
        services.AddSystemModellerEdgeRepo();
        services.AddSystemModellerEdgeService();
        services.AddVehiclePriorityEdgeRepos();
        services.AddVehiclePriorityEdgeService( options =>
        {
            options.DefaultChannel = context.Configuration["Topics:ConfigPriorityResponse"] ??
                                     throw new NullReferenceException(
                                         "Topics:ConfigPriorityResponse missing in config");
        });
        services.AddDetectorCount();
        services.AddHostedService<IntersectionConfigWorker>();
        services.AddHostedService<DataIngesterHandler>();
    })
    .Build()
    .LogStartup()
    .RunAsync();
