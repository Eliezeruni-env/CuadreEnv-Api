using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Onion.BussinesLogic.Background
{
    public class BackgroundWorker : BackgroundService
    {
        private readonly InMemoryBackgroundQueue _queue;
        private readonly ILogger<BackgroundWorker> _logger;

        public BackgroundWorker(InMemoryBackgroundQueue queue, ILogger<BackgroundWorker> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background worker started");
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var job))
                {
                    try
                    {
                        _logger.LogInformation("Processing job {Type}", job.Type);
                        // Process job: in real scenario dispatch to handlers
                        await Task.Delay(100, stoppingToken);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Error processing job {Type}", job.Type);
                    }
                }
                else
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
        }
    }
}
