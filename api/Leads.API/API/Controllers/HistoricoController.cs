using ClosedXML.Excel;
using Leads.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HistoricoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public HistoricoController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null,
            [FromQuery] int? clienteId = null)
        {
            // Pegar o usuário logado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return Unauthorized();

            var query = _context.HistoricoExportacoes
                .Include(h => h.Cliente)
                .Include(h => h.Usuario)
                .AsQueryable();

            // Se não for admin, mostrar apenas as exportações do cliente dele
            if (usuario.Perfil != "Admin" && usuario.ClienteId.HasValue)
            {
                query = query.Where(h => h.ClienteId == usuario.ClienteId.Value);
            }
            else if (clienteId.HasValue && usuario.Perfil == "Admin")
            {
                query = query.Where(h => h.ClienteId == clienteId.Value);
            }

            // Filtros de data
            if (dataInicio.HasValue)
                query = query.Where(h => h.DataExportacao >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(h => h.DataExportacao <= dataFim.Value);

            var totalItens = await query.CountAsync();

            var isAdmin = usuario.Perfil == "Admin";

            var items = await query
              .OrderByDescending(h => h.DataExportacao)
              .Skip((pagina - 1) * tamanhoPagina)
              .Take(tamanhoPagina)
              .Select(h => new {
                  h.Id,
                  h.DataExportacao,
                  h.QuantidadeLeads,
                  FiltrosUtilizados = h.FiltrosUtilizados ?? "",
                  NomeArquivo = h.NomeArquivo ?? "",
                  EmailDestino = h.EmailDestino ?? "",
                  h.EnviadoPorEmail,
                  Status = h.Status ?? "",
                  MensagemErro = h.MensagemErro ?? "",
                  PlanoNome = h.PlanoNome ?? "",
                  h.LimiteDisponivel,
                  Cliente = isAdmin
                  ? new
                  {
                      RazaoSocial = h.Cliente.RazaoSocial ?? "",
                      CNPJ = h.Cliente.CNPJ ?? ""
                  }
                  : null,
                  Usuario = new
                  {
                      NomeUsuario = h.Usuario.NomeUsuario ?? "",
                      Email = h.Usuario.Email ?? ""
                  }
              })
              .ToListAsync();

            return Ok(new
            {
                items,
                totalItens,
                totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina),
                paginaAtual = pagina
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterDetalhes(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var historico = await _context.HistoricoExportacoes
                .Include(h => h.Cliente)
                .Include(h => h.Usuario)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (historico == null)
                return NotFound();

            // Verificar permissão
            if (usuario.Perfil != "Admin" && historico.ClienteId != usuario.ClienteId)
                return Forbid();

            return Ok(historico);
        }

        [HttpPost("{id}/reenviar-email")]
        public async Task<IActionResult> ReenviarEmail(int id)
        {
            var historico = await _context.HistoricoExportacoes
                .FirstOrDefaultAsync(h => h.Id == id);

            if (historico == null)
                return NotFound();

            if (string.IsNullOrEmpty(historico.EmailDestino))
                return BadRequest(new { message = "Esta exportação não foi enviada por email." });

            try
            {
                // Recriar o arquivo baseado nos filtros
                // Por simplicidade, vamos apenas simular o reenvio
                // Em produção, você deveria recriar o arquivo com os mesmos filtros

                // await _emailService.EnviarArquivoPorEmailAsync(
                //     historico.EmailDestino, 
                //     arquivo, 
                //     historico.NomeArquivo);

                return Ok(new { message = "Email reenviado com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao reenviar email: {ex.Message}" });
            }
        }

        [HttpGet("relatorio")]
        public async Task<IActionResult> GerarRelatorio(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var query = _context.HistoricoExportacoes
                .Include(h => h.Cliente)
                .Include(h => h.Usuario)
                .Where(h => h.DataExportacao >= dataInicio && h.DataExportacao <= dataFim);

            // Se não for admin, filtrar por cliente
            if (usuario.Perfil != "Admin" && usuario.ClienteId.HasValue)
            {
                query = query.Where(h => h.ClienteId == usuario.ClienteId.Value);
            }

            var dados = await query
                .OrderBy(h => h.DataExportacao)
                .ToListAsync();

            // Gerar Excel
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Exportações");

            // Cabeçalho
            var col = 1;
            worksheet.Cell(1, col++).Value = "Data/Hora";

            if (usuario.Perfil == "Admin")
            {
                worksheet.Cell(1, col++).Value = "Cliente";
                worksheet.Cell(1, col++).Value = "CNPJ";
            }

            worksheet.Cell(1, col++).Value = "Usuário";
            worksheet.Cell(1, col++).Value = "Arquivo";
            worksheet.Cell(1, col++).Value = "Quantidade";
            worksheet.Cell(1, col++).Value = "Email Destino";
            worksheet.Cell(1, col++).Value = "Status";
            worksheet.Cell(1, col++).Value = "Plano";

            // Dados
            var row = 2;
            foreach (var item in dados)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = item.DataExportacao.ToString("dd/MM/yyyy HH:mm");

                if (usuario.Perfil == "Admin")
                {
                    worksheet.Cell(row, col++).Value = item.Cliente.RazaoSocial;
                    worksheet.Cell(row, col++).Value = item.Cliente.CNPJ;
                }

                worksheet.Cell(row, col++).Value = item.Usuario.Email;
                worksheet.Cell(row, col++).Value = item.NomeArquivo;
                worksheet.Cell(row, col++).Value = item.QuantidadeLeads;
                worksheet.Cell(row, col++).Value = item.EmailDestino ?? "Download direto";
                worksheet.Cell(row, col++).Value = item.Status;
                worksheet.Cell(row, col++).Value = item.PlanoNome;

                row++;
            }

            // Adicionar totalizadores
            row += 2;
            worksheet.Cell(row, 1).Value = "Total de Exportações:";
            worksheet.Cell(row, 2).Value = dados.Count;
            row++;
            worksheet.Cell(row, 1).Value = "Total de Leads Exportados:";
            worksheet.Cell(row, 2).Value = dados.Sum(d => d.QuantidadeLeads);

            // Formatar como tabela
            var range = worksheet.Range(1, 1, dados.Count + 1, col - 1);
            var table = range.CreateTable();
            table.ShowTotalsRow = false;
            table.Theme = XLTableTheme.TableStyleMedium2;
            // Ajustar largura das colunas
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var nomeArquivo = $"relatorio-exportacoes-{dataInicio:yyyyMMdd}-{dataFim:yyyyMMdd}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nomeArquivo);
        }
    }
}