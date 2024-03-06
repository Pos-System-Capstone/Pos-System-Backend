using Pos_System.API.Services.Interfaces;


namespace Pos_System.API.Services;

public class BackgroundJobService : BackgroundService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IUserService _userService;

    public BackgroundJobService(ILogger<BackgroundJobService> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _userService.UpdateUserPoint();
                await Task.Delay(3600000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error: " + ex.Message);
            await Task.Delay(600000, stoppingToken); //.ConfigureAwait(false);
            await ExecuteAsync(stoppingToken);
        }
    }
}