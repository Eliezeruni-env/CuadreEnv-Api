using System.Threading;
using System.Threading.Tasks;

namespace Onion.Common.Services
{
    public interface IAgendaNotificationPublisher
    {
        Task PublishAppointmentCreatedAsync(int companyId, object data, CancellationToken cancellationToken = default);
        Task PublishAppointmentStatusChangedAsync(int companyId, object data, CancellationToken cancellationToken = default);
    }
}
