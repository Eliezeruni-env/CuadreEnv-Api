using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Onion.Common.Services;
using crud_onion.Hubs;

namespace crud_onion.Services
{
    public sealed class SignalRAgendaNotificationPublisher : IAgendaNotificationPublisher
    {
        private readonly IHubContext<SecurityAlertsHub> _hub;

        public SignalRAgendaNotificationPublisher(IHubContext<SecurityAlertsHub> hub)
        {
            _hub = hub;
        }

        public Task PublishAppointmentCreatedAsync(int companyId, object data, CancellationToken cancellationToken = default) =>
            _hub.Clients.Group($"company:{companyId}").SendAsync("AppointmentCreated", new
            {
                companyId,
                data,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

        public Task PublishAppointmentStatusChangedAsync(int companyId, object data, CancellationToken cancellationToken = default) =>
            _hub.Clients.Group($"company:{companyId}").SendAsync("AppointmentStatusChanged", new
            {
                companyId,
                data,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);
    }
}
