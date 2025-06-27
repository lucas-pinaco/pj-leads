
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
namespace Leads.API.Application.Services
{
    [Authorize]
    public class ImportProgressHub : Hub
    {
        private readonly ILogger<ImportProgressHub> _logger;
        private readonly IBackgroundJobService _jobService;

        public ImportProgressHub(ILogger<ImportProgressHub> logger, IBackgroundJobService jobService)
        {
            _logger = logger;
            _jobService = jobService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("Usuário {UserId} conectado ao hub", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("Usuário {UserId} desconectado do hub", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Método para cliente solicitar status de um job específico
        public async Task GetJobStatus(string jobId)
        {
            var status = await _jobService.GetJobStatusAsync(jobId);
            if (status != null)
            {
                await Clients.Caller.SendAsync("JobStatusUpdate", status);
            }
        }

        // Método para cliente solicitar todos os seus jobs
        public async Task GetMyJobs()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var jobs = await _jobService.GetUserJobsAsync(userId);
                await Clients.Caller.SendAsync("UserJobsUpdate", jobs);
            }
        }

        // Método para entrar em um grupo específico de job
        public async Task JoinJobGroup(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Job_{jobId}");
        }

        // Método para sair de um grupo específico de job
        public async Task LeaveJobGroup(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Job_{jobId}");
        }
    }
}