using Leads.API.Application.Services;
using Leads.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            IBackgroundJobService backgroundJobService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        /// <summary>
        /// Upload com processamento em background
        /// </summary>
        [HttpPost("upload-background")]
        public async Task<IActionResult> UploadBackground()
        {
            var arquivos = Request.Form.Files;
            if (arquivos == null || arquivos.Count == 0)
            {
                return BadRequest(new { message = "Nenhum arquivo foi enviado." });
            }

            // Validar arquivos
            var arquivosValidos = new List<ArquivoUpload>();
            var erros = new List<string>();

            foreach (var formFile in arquivos)
            {
                if (formFile.Length == 0)
                {
                    erros.Add($"Arquivo {formFile.FileName} está vazio.");
                    continue;
                }

                var ext = Path.GetExtension(formFile.FileName).ToLower();
                if (ext != ".csv" && ext != ".xlsx" && ext != ".xls")
                {
                    erros.Add($"Arquivo {formFile.FileName} não é um formato suportado (CSV, XLSX, XLS).");
                    continue;
                }

                // Limite de 50MB por arquivo
                if (formFile.Length > 50 * 1024 * 1024)
                {
                    erros.Add($"Arquivo {formFile.FileName} excede o limite de 50MB.");
                    continue;
                }

                // Converter para bytes
                using var ms = new MemoryStream();
                await formFile.CopyToAsync(ms);

                arquivosValidos.Add(new ArquivoUpload
                {
                    Nome = formFile.FileName,
                    ContentType = formFile.ContentType,
                    Data = ms.ToArray(),
                    Tamanho = formFile.Length
                });
            }

            if (!arquivosValidos.Any())
            {
                return BadRequest(new
                {
                    message = "Nenhum arquivo válido encontrado.",
                    erros
                });
            }

            // Obter usuário atual
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                // Iniciar job em background
                var jobId = await _backgroundJobService.StartImportJobAsync(userId, arquivosValidos);

                return Ok(new
                {
                    message = "Importação iniciada em background.",
                    jobId,
                    totalArquivos = arquivosValidos.Count,
                    arquivosProcessados = arquivosValidos.Select(a => a.Nome).ToList(),
                    erros = erros.Any() ? erros : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar job de importação");
                return StatusCode(500, new
                {
                    message = "Erro interno do servidor ao iniciar importação."
                });
            }
        }

        /// <summary>
        /// Consultar status de um job
        /// </summary>
        [HttpGet("job-status/{jobId}")]
        public async Task<IActionResult> GetJobStatus(string jobId)
        {
            var status = await _backgroundJobService.GetJobStatusAsync(jobId);
            if (status == null)
            {
                return NotFound(new { message = "Job não encontrado." });
            }

            return Ok(status);
        }

        /// <summary>
        /// Listar jobs do usuário atual
        /// </summary>
        [HttpGet("my-jobs")]
        public async Task<IActionResult> GetMyJobs()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var jobs = await _backgroundJobService.GetUserJobsAsync(userId);
            return Ok(jobs);
        }

        /// <summary>
        /// Upload síncrono (método original para compatibilidade)
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadArquivos()
        {
            var arquivos = Request.Form.Files;
            if (arquivos == null || arquivos.Count == 0)
            {
                return BadRequest(new { message = "Nenhum arquivo foi enviado." });
            }

            var totalLeadsImportados = 0;
            var arquivosProcessados = new List<string>();
            var erros = new List<string>();

            foreach (var formFile in arquivos)
            {
                if (formFile.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        await formFile.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();

                        // Salvar arquivo no banco
                        var novoArquivo = new Arquivo
                        {
                            Nome = Path.GetFileName(formFile.FileName),
                            ContentType = formFile.ContentType,
                            Data = fileBytes,
                            DataUpload = DateTime.UtcNow
                        };

                        _context.Arquivos.Add(novoArquivo);
                        await _context.SaveChangesAsync();

                        // Processar leads do arquivo
                        var leads = new List<Lead>();
                        var ext = Path.GetExtension(formFile.FileName).ToLower();

                        ms.Position = 0;

                        if (ext == ".csv")
                        {
                            leads = CsvHelperUtil.LerLeadsCsv(ms);
                        }
                        else if (ext == ".xlsx" || ext == ".xls")
                        {
                            leads = ExcelHelper.LerLeadsExcel(ms);
                        }
                        else
                        {
                            erros.Add($"Formato não suportado: {formFile.FileName}");
                            continue;
                        }

                        // Importar leads usando o serviço existente
                        var importacaoService = HttpContext.RequestServices.GetRequiredService<ImportacaoService>();
                        await importacaoService.ImportarLeadsAsync(leads);

                        totalLeadsImportados += leads.Count;
                        arquivosProcessados.Add(formFile.FileName);

                        // Atualizar arquivo com informações de processamento
                        novoArquivo.ProcessadoEm = DateTime.UtcNow;
                        novoArquivo.QuantidadeLeads = leads.Count;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar arquivo {FileName}", formFile.FileName);
                        erros.Add($"Erro ao processar {formFile.FileName}: {ex.Message}");
                    }
                }
            }

            return Ok(new
            {
                message = $"{arquivos.Count} arquivo(s) enviado(s). {arquivosProcessados.Count} processado(s) com sucesso.",
                totalLeadsImportados,
                arquivosProcessados,
                erros = erros.Any() ? erros : null
            });
        }

        /// <summary>
        /// Lista arquivos com informações de processamento
        /// </summary>
        [HttpGet("listar")]
        public async Task<IActionResult> ListarArquivos([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Arquivos.AsNoTracking();

            var total = await query.CountAsync();
            var arquivos = await query
                .OrderByDescending(a => a.DataUpload)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Nome,
                    a.ContentType,
                    a.DataUpload,
                    a.ProcessadoEm,
                    a.QuantidadeLeads,
                    a.ErroProcessamento,
                    Processado = a.ProcessadoEm != null,
                    Sucesso = a.ErroProcessamento == null,
                    TamanhoFormatado = FormatFileSize(a.Data.Length)
                })
                .ToListAsync();

            return Ok(new
            {
                arquivos,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        /// <summary>
        /// Download de arquivo por ID
        /// </summary>
        [HttpGet("download/{id:int}")]
        public async Task<IActionResult> Download(int id)
        {
            var arquivo = await _context.Arquivos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (arquivo == null)
                return NotFound(new { message = "Arquivo não encontrado." });

            return File(arquivo.Data, arquivo.ContentType, arquivo.Nome);
        }

        /// <summary>
        /// Reprocessa um arquivo específico
        /// </summary>
        [HttpPost("reprocessar/{id:int}")]
        public async Task<IActionResult> Reprocessar(int id)
        {
            var arquivo = await _context.Arquivos.FindAsync(id);
            if (arquivo == null)
                return NotFound(new { message = "Arquivo não encontrado." });

            try
            {
                var leads = new List<Lead>();
                var ext = Path.GetExtension(arquivo.Nome).ToLower();

                using var ms = new MemoryStream(arquivo.Data);

                if (ext == ".csv")
                {
                    leads = CsvHelperUtil.LerLeadsCsv(ms);
                }
                else if (ext == ".xlsx" || ext == ".xls")
                {
                    leads = ExcelHelper.LerLeadsExcel(ms);
                }
                else
                {
                    return BadRequest(new { message = "Formato de arquivo não suportado." });
                }

                var importacaoService = HttpContext.RequestServices.GetRequiredService<ImportacaoService>();
                await importacaoService.ImportarLeadsAsync(leads);

                arquivo.ProcessadoEm = DateTime.UtcNow;
                arquivo.QuantidadeLeads = leads.Count;
                arquivo.ErroProcessamento = null;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Arquivo reprocessado com sucesso.",
                    quantidadeLeads = leads.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reprocessar arquivo {Id}", id);

                arquivo.ErroProcessamento = ex.Message;
                await _context.SaveChangesAsync();

                return BadRequest(new { message = $"Erro ao reprocessar: {ex.Message}" });
            }
        }

        /// <summary>
        /// Estatísticas de importação
        /// </summary>
        [HttpGet("estatisticas")]
        public async Task<IActionResult> GetEstatisticas()
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);

            var stats = new
            {
                TotalArquivos = await _context.Arquivos.CountAsync(),
                ArquivosHoje = await _context.Arquivos.CountAsync(a => a.DataUpload.Date == hoje),
                ArquivosSemana = await _context.Arquivos.CountAsync(a => a.DataUpload >= inicioSemana),
                ArquivosMes = await _context.Arquivos.CountAsync(a => a.DataUpload >= inicioMes),

                TotalLeads = await _context.Leads.CountAsync(),
                LeadsHoje = await _context.Arquivos
                    .Where(a => a.DataUpload.Date == hoje && a.QuantidadeLeads.HasValue)
                    .SumAsync(a => a.QuantidadeLeads.Value),
                LeadsSemana = await _context.Arquivos
                    .Where(a => a.DataUpload >= inicioSemana && a.QuantidadeLeads.HasValue)
                    .SumAsync(a => a.QuantidadeLeads.Value),
                LeadsMes = await _context.Arquivos
                    .Where(a => a.DataUpload >= inicioMes && a.QuantidadeLeads.HasValue)
                    .SumAsync(a => a.QuantidadeLeads.Value),

                ArquivosComErro = await _context.Arquivos.CountAsync(a => !string.IsNullOrEmpty(a.ErroProcessamento)),
                UltimaImportacao = await _context.Arquivos
                    .OrderByDescending(a => a.DataUpload)
                    .Select(a => a.DataUpload)
                    .FirstOrDefaultAsync()
            };

            return Ok(stats);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}