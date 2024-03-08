// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Collections.Concurrent;
using Econolite.Ode.Models.Entities;
using Econolite.Ode.Models.Entities.Types;
using Econolite.OdeRepository.SystemModeller;

namespace Econolite.Ode.Domain.Detector;

public class VehicleCountService
{
    private readonly IEntityNodeJsonFileRepository _entityNodeJsonFileRepository;
    private readonly ConcurrentDictionary<string, int> _vehicleCounts = new();
    public VehicleCountService(IEntityNodeJsonFileRepository entityNodeJsonFileRepository)
    {
        _entityNodeJsonFileRepository = entityNodeJsonFileRepository;
    }

    public async Task<(int DetectorId, bool Count)> CountVehicle(string vehicleId, GeoJsonPointFeature point)
    {
        var result = (0, false);
        var detectors = await GetDetectorsAsync(point);
        if (!detectors.Any())
        {
            Remove(vehicleId);
            return result;
        }
        
        var detectorId = detectors.First().IdMapping;
        if (!detectorId.HasValue) return result;
        
        var resultId = GetDetectorId(vehicleId);
        if (resultId == detectorId)
        {
            return (detectorId.Value, false);
        }
        Add(vehicleId, detectorId.Value);
        return (detectorId.Value, true);
    }
    
    private void Add(string vehicleId, int detectorId)
    {
        _vehicleCounts.AddOrUpdate(vehicleId, detectorId, (key, value) => value);
    }
    
    private void Remove(string vehicleId)
    {
        _vehicleCounts.TryRemove(vehicleId, out _);
    }
    
    private int GetDetectorId(string vehicleId)
    {
        return _vehicleCounts.TryGetValue(vehicleId, out var detectorId) ? detectorId : 0;
    }
    
    private async Task<EntityNode[]> GetDetectorsAsync(GeoJsonPointFeature point)
    {
        var results = await _entityNodeJsonFileRepository.QueryIntersectingGeoFences(point);
        return results.Where(r => r.Type.Id == DetectorTypeId.Id).ToArray();
    }
}