using System.Threading.Channels;
using Gateway.API.Models;
using Microsoft.Extensions.Hosting;

namespace Gateway.API.Contracts;

/// <summary>
/// Interface for encounter polling background services.
/// </summary>
public interface IEncounterPollingService : IHostedService
{
    /// <summary>
    /// Gets the channel reader for consuming encounter completion events.
    /// </summary>
    ChannelReader<EncounterCompletedEvent> Encounters { get; }
}
