using Backend.Negocio.Pipeline;
using Backend.Utils;

namespace Backend.Services
{
    public sealed class AudioRetentionWorker : BackgroundService
    {
        private readonly AudioRetentionRepository _repo = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var softDays = AppConfig.AUDIO_RETENTION_SOFT_DAYS;
                    var purgeDays = AppConfig.AUDIO_RETENTION_PURGE_DAYS;

                    if (softDays > 0)
                    {
                        var count = _repo.SoftDeleteOlderThan(softDays);
                        if (count > 0)
                        {
                            Console.WriteLine($"[Retention] Soft-deleted {count} audios.");
                        }
                    }

                    if (purgeDays > 0)
                    {
                        var iterations = 0;
                        const int batch = 200;
                        while (iterations < 10)
                        {
                            var candidates = _repo.GetPurgeCandidates(purgeDays, batch);
                            if (candidates.Count == 0)
                                break;

                            foreach (var item in candidates)
                            {
                                if (!string.IsNullOrWhiteSpace(item.FilePath))
                                    AudioStorage.Delete(item.FilePath);
                                _repo.HardDelete(item.Id);
                            }

                            iterations++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Retention] Error: {ex.Message}");
                }

                var delay = TimeSpan.FromMinutes(Math.Max(5, AppConfig.AUDIO_RETENTION_RUN_MINUTES));
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
