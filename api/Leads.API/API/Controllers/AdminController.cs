using global::Leads.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ImportacaoService _importacaoService;

        public AdminController(AppDbContext context, ImportacaoService importacaoService)
        {
            _context = context;
            _importacaoService = importacaoService;
        }

        /// <summary>
        /// Faz upload de um ou mais arquivos e processa os leads
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

            foreach (var formFile in arquivos)
            {
                if (formFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await formFile.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();

                    // Salva o arquivo no banco
                    var novoArquivo = new Arquivo
                    {
                        Nome = Path.GetFileName(formFile.FileName),
                        ContentType = formFile.ContentType,
                        Data = fileBytes,
                        DataUpload = DateTime.UtcNow
                    };

                    _context.Arquivos.Add(novoArquivo);
                    await _context.SaveChangesAsync();

                    // Processa os leads do arquivo
                    try
                    {
                        var leads = new List<Lead>();
                        var ext = Path.GetExtension(formFile.FileName).ToLower();

                        // Reseta o stream para leitura
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
                            continue; // Pula arquivos não suportados
                        }

                        // Importa os leads
                        await _importacaoService.ImportarLeadsAsync(leads);
                        totalLeadsImportados += leads.Count;
                        arquivosProcessados.Add(formFile.FileName);

                        // Atualiza o arquivo com informações de processamento
                        novoArquivo.ProcessadoEm = DateTime.UtcNow;
                        novoArquivo.QuantidadeLeads = leads.Count;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log do erro (idealmente usar ILogger)
                        novoArquivo.ErroProcessamento = ex.Message;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok(new
            {
                message = $"{arquivos.Count} arquivo(s) enviado(s). {arquivosProcessados.Count} processado(s) com sucesso.",
                totalLeadsImportados,
                arquivosProcessados
            });
        }

        /// <summary>
        /// Lista arquivos com informações de processamento
        /// </summary>
        [HttpGet("listar")]
        public async Task<IActionResult> ListarArquivos()
        {
            var lista = await _context.Arquivos
                .AsNoTracking()
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
                    Sucesso = a.ErroProcessamento == null
                })
                .OrderByDescending(a => a.DataUpload)
                .ToListAsync();

            return Ok(lista);
        }

        /// <summary>
        /// Permite fazer download de um arquivo específico pelo Id
        /// </summary>
        [HttpGet("download/{id:int}")]
        public async Task<IActionResult> Download(int id)
        {
            var a = await _context.Arquivos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null)
                return NotFound(new { message = "Arquivo não encontrado." });

            return File(a.Data, a.ContentType, a.Nome);
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

                await _importacaoService.ImportarLeadsAsync(leads);

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
                arquivo.ErroProcessamento = ex.Message;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = $"Erro ao reprocessar: {ex.Message}" });
            }
        }
    }
}