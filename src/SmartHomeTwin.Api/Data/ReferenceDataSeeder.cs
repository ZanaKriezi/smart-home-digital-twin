using SmartHomeTwin.Core.Devices;
using SmartHomeTwin.Core.Scenarios;

namespace SmartHomeTwin.Api.Data;

///Keeps the Devices and Scenarios tables in line with the configuration files.
public static class ReferenceDataSeeder
{
    public static void Seed(TwinDbContext db, HomeTwin home, List<ScenarioDefinition> scenarios)
    {
        var devices = db.Devices.ToDictionary(d => d.Id);
        foreach (var d in home.Devices)
        {
            if (!devices.TryGetValue(d.Id, out var entity))
                db.Devices.Add(entity = new DeviceEntity { Id = d.Id });
            entity.Name = d.Name;
            entity.Type = d.Type;
            entity.Category = d.Category.ToString();
            entity.RatedPowerKw = d.RatedPowerKw;
        }

        var existing = db.Scenarios.ToDictionary(s => s.Id);
        foreach (var s in scenarios)
        {
            if (!existing.TryGetValue(s.Id, out var entity))
                db.Scenarios.Add(entity = new ScenarioEntity { Id = s.Id });
            entity.Name = s.Name;
            entity.Description = s.Description;
            entity.Seed = s.Seed;
            entity.StartDate = s.StartDate;
        }

        db.SaveChanges();
    }
}
