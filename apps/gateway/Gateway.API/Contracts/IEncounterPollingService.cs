using Microsoft.Extensions.Hosting;

namespace Gateway.API.Contracts;

/// <summary>
/// Marker interface for encounter polling background services.
/// </summary>
public interface IEncounterPollingService : IHostedService
{
}
