// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Microsoft.Extensions.DependencyInjection;

namespace Econolite.Ode.Domain.Detector.Extensions;

public static class Defined
{
    public static IServiceCollection AddDetectorCount(this IServiceCollection services)
    {
        services.AddTransient<VehicleCountService>();
        services.AddTransient<VehicleBsmHandler>();

        return services;
    }
}