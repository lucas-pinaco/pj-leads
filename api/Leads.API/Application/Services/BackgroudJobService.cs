
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;
using Leads.API.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Leads.API.Application.Services
{
    public interface IBackgroundJobService
    {
        Task<string> StartImportJobAsync(int usuarioId, List<ArquivoUpload> arquivos);
        Task<JobStatus> GetJobStatusAsync(string jobId);
        Task<List<JobStatus>> GetUserJobsAsync(int usuarioId);
    }

    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly ILogger<BackgroundJobService> _logger;

        // Armazenamento em memória dos jobs (em produção, usar Redis ou banco)
        private static readonly ConcurrentDictionary<string, JobStatus> _jobs = new();

        public BackgroundJobService(
            IServiceProvider serviceProvider,
            IHubContext<ImportProgressHub> hubContext,
            ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<string> StartImportJobAsync(int usuarioId, List<ArquivoUpload> arquivos)
        {
            var jobId = Guid.NewGuid().ToString();

            var jobStatus = new JobStatus
            {
                JobId = jobId,
                UsuarioId = usuarioId,
                Status = JobStatusEnum.Iniciado,
                TotalArquivos = arquivos.Count,
                ArquivosProcessados = 0,
                TotalLeads = 0,
                LeadsProcessados = 0,
                Iniciado = DateTime.UtcNow,
                Arquivos = arquivos.Select(a => new ArquivoJobStatus
                {
                    Nome = a.Nome,
                    Tamanho = a.Tamanho,
                    Status = ArquivoStatusEnum.Aguardando
                }).ToList()
            };

            _jobs[jobId] = jobStatus;

            // Iniciar processamento em background
            _ = Task.Run(async () => await ProcessImportJobAsync(jobId, arquivos));

            // Notificar usuário que job foi iniciado
            await _hubContext.Clients.User(usuarioId.ToString())
                .SendAsync("JobStatusUpdate", jobStatus);

            return jobId;
        }

        public Task<JobStatus> GetJobStatusAsync(string jobId)
        {
            _jobs.TryGetValue(jobId, out var status);
            return Task.FromResult(status);
        }

        public Task<List<JobStatus>> GetUserJobsAsync(int usuarioId)
        {
            var userJobs = _jobs.Values
                .Where(j => j.UsuarioId == usuarioId)
                .OrderByDescending(j => j.Iniciado)
                .ToList();

            return Task.FromResult(userJobs);
        }

        private async Task ProcessImportJobAsync(string jobId, List<ArquivoUpload> arquivos)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var importacaoService = scope.ServiceProvider.GetRequiredService<ImportacaoService>();

            var jobStatus = _jobs[jobId];

            try
            {
                jobStatus.Status = JobStatusEnum.Processando;
                await NotifyJobUpdate(jobStatus);

                foreach (var arquivo in arquivos)
                {
                    await ProcessSingleFileAsync(context, importacaoService, jobStatus, arquivo);
                }

                jobStatus.Status = JobStatusEnum.Concluido;
                jobStatus.Finalizado = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar job {JobId}", jobId);

                jobStatus.Status = JobStatusEnum.Erro;
                jobStatus.MensagemErro = ex.Message;
                jobStatus.Finalizado = DateTime.UtcNow;
            }

            await NotifyJobUpdate(jobStatus);
        }

        private async Task ProcessSingleFileAsync(
            AppDbContext context,
            ImportacaoService importacaoService,
            JobStatus jobStatus,
            ArquivoUpload arquivoUpload)
        {
            var arquivoStatus = jobStatus.Arquivos.First(a => a.Nome == arquivoUpload.Nome);

            try
            {
                arquivoStatus.Status = ArquivoStatusEnum.Processando;
                arquivoStatus.Iniciado = DateTime.UtcNow;
                await NotifyJobUpdate(jobStatus);

                // Salvar arquivo no banco
                var arquivo = new Arquivo
                {
                    Nome = arquivoUpload.Nome,
                    ContentType = arquivoUpload.ContentType,
                    Data = arquivoUpload.Data,
                    DataUpload = DateTime.UtcNow
                };

                context.Arquivos.Add(arquivo);
                await context.SaveChangesAsync();

                // Processar leads do arquivo
                var leads = new List<Lead>();
                var ext = Path.GetExtension(arquivoUpload.Nome).ToLower();

                using var stream = new MemoryStream(arquivoUpload.Data);

                if (ext == ".csv")
                {
                    leads = CsvHelperUtil.LerLeadsCsv(stream);
                }
                else if (ext == ".xlsx" || ext == ".xls")
                {
                    leads = ExcelHelper.LerLeadsExcel(stream);
                }

                arquivoStatus.TotalLeads = leads.Count;
                await NotifyJobUpdate(jobStatus);

                // Processar leads em lotes para melhor performance
                const int batchSize = 100;
                var processedCount = 0;

                for (int i = 0; i < leads.Count; i += batchSize)
                {
                    var batch = leads.Skip(i).Take(batchSize).ToList();
                    await importacaoService.ImportarLeadsAsync(batch);

                    processedCount += batch.Count;
                    arquivoStatus.LeadsProcessados = processedCount;
                    jobStatus.LeadsProcessados += batch.Count;

                    // Atualizar progresso a cada lote
                    await NotifyJobUpdate(jobStatus);

                    // Pequena pausa para não sobrecarregar
                    await Task.Delay(50);
                }

                arquivoStatus.Status = ArquivoStatusEnum.Concluido;
                arquivoStatus.Finalizado = DateTime.UtcNow;

                // Atualizar informações do arquivo no banco
                arquivo.ProcessadoEm = DateTime.UtcNow;
                arquivo.QuantidadeLeads = leads.Count;
                await context.SaveChangesAsync();

                jobStatus.ArquivosProcessados++;
                jobStatus.TotalLeads += leads.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar arquivo {FileName}", arquivoUpload.Nome);

                arquivoStatus.Status = ArquivoStatusEnum.Erro;
                arquivoStatus.MensagemErro = ex.Message;
                arquivoStatus.Finalizado = DateTime.UtcNow;
            }

            await NotifyJobUpdate(jobStatus);
        }

        private async Task NotifyJobUpdate(JobStatus jobStatus)
        {
            await _hubContext.Clients.User(jobStatus.UsuarioId.ToString())
                .SendAsync("JobStatusUpdate", jobStatus);
        }
    }

    // Models para o job
    public class JobStatus
    {
        public string JobId { get; set; }
        public int UsuarioId { get; set; }
        public JobStatusEnum Status { get; set; }
        public int TotalArquivos { get; set; }
        public int ArquivosProcessados { get; set; }
        public int TotalLeads { get; set; }
        public int LeadsProcessados { get; set; }
        public DateTime Iniciado { get; set; }
        public DateTime? Finalizado { get; set; }
        public string MensagemErro { get; set; }
        public List<ArquivoJobStatus> Arquivos { get; set; } = new();

        public int ProgressoPercentual => TotalLeads > 0 ? (int)((double)LeadsProcessados / TotalLeads * 100) : 0;
        public TimeSpan? TempoDecorrido => Finalizado?.Subtract(Iniciado) ?? DateTime.UtcNow.Subtract(Iniciado);
    }

    public class ArquivoJobStatus
    {
        public string Nome { get; set; }
        public long Tamanho { get; set; }
        public ArquivoStatusEnum Status { get; set; }
        public int TotalLeads { get; set; }
        public int LeadsProcessados { get; set; }
        public DateTime? Iniciado { get; set; }
        public DateTime? Finalizado { get; set; }
        public string MensagemErro { get; set; }

        public int ProgressoPercentual => TotalLeads > 0 ? (int)((double)LeadsProcessados / TotalLeads * 100) : 0;
    }

    public class ArquivoUpload
    {
        public string Nome { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public long Tamanho { get; set; }
    }

    public enum JobStatusEnum
    {
        Iniciado,
        Processando,
        Concluido,
        Erro,
        Cancelado
    }

    public enum ArquivoStatusEnum
    {
        Aguardando,
        Processando,
        Concluido,
        Erro
    }
}