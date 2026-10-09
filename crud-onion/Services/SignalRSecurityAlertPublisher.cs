using Microsoft.AspNetCore.SignalR;
using Onion.Common.Services;
using crud_onion.Hubs;

namespace crud_onion.Services;

public sealed class SignalRSecurityAlertPublisher : ISecurityAlertPublisher
{
    private readonly IHubContext<SecurityAlertsHub> _hub;

    public SignalRSecurityAlertPublisher(IHubContext<SecurityAlertsHub> hub) => _hub = hub;

    public Task PublishAsync(string eventCode, int companyId, object data, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group($"company:{companyId}").SendAsync("securityAlert", new
        {
            eventCode,
            companyId,
            data,
            occurredAtUtc = DateTime.UtcNow
        }, cancellationToken);
}
