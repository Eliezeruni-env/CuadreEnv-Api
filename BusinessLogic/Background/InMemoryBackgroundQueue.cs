using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Onion.BussinesLogic.Background
{
    public class InMemoryBackgroundQueue : IBackgroundQueue
    {
        private readonly ConcurrentQueue<BackgroundJob> _queue = new ConcurrentQueue<BackgroundJob>();
        private readonly ILogger<InMemoryBackgroundQueue>? _logger;

        public InMemoryBackgroundQueue(ILogger<InMemoryBackgroundQueue>? logger = null)
        {
            _logger = logger;
        }

        public void Enqueue(BackgroundJob job)
        {
            _queue.Enqueue(job);
            _logger?.LogInformation("Enqueued background job {Type}", job.Type);
        }

        public bool TryDequeue(out BackgroundJob job)
        {
            return _queue.TryDequeue(out job!);
        }
    }
}
