using System;
namespace Onion.BussinesLogic.Background
{
    public interface IBackgroundQueue
    {
        void Enqueue(BackgroundJob job);
    }

    public class BackgroundJob
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
