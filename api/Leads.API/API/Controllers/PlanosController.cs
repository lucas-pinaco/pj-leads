using Leads.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlanosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] // Permite listar planos sem autenticação
        public async Task<IActionResult> Listar()
        {
            var planos = await _context.Planos
                .Where(p => p.Ativo)
                .OrderBy(p => p.OrdemExibicao)
                .ThenBy(p => p.Valor)
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    p.Descricao,
                    p.Valor,
                    p.LimiteExportacoesMes,
                    p.LimiteLeadsPorExportacao,
                    p.PermiteExportarEmail,
                    p.PermiteExportarTelefone,
                    p.PermiteFiltrosAvancados,
                    p.SuportePrioritario,
                    p.OrdemExibicao
                })
                .ToListAsync();

            return Ok(planos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var plano = await _context.Planos
                .Where(p => p.Id == id && p.Ativo)
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    p.Descricao,
                    p.Valor,
                    p.LimiteExportacoesMes,
                    p.LimiteLeadsPorExportacao,
                    p.PermiteExportarEmail,
                    p.PermiteExportarTelefone,
                    p.PermiteFiltrosAvancados,
                    p.SuportePrioritario,
                    p.OrdemExibicao,
                    p.DataCriacao
                })
                .FirstOrDefaultAsync();

            if (plano == null)
                return NotFound();

            return Ok(plano);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] CriarPlanoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var plano = new Plano
            {
                Nome = request.Nome,
                Descricao = request.Descricao,
                Valor = request.Valor,
                LimiteExportacoesMes = request.LimiteExportacoesMes,
                LimiteLeadsPorExportacao = request.LimiteLeadsPorExportacao,
                PermiteExportarEmail = request.PermiteExportarEmail,
                PermiteExportarTelefone = request.PermiteExportarTelefone,
                PermiteFiltrosAvancados = request.PermiteFiltrosAvancados,
                SuportePrioritario = request.SuportePrioritario,
                OrdemExibicao = request.OrdemExibicao,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            _context.Planos.Add(plano);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = plano.Id }, plano);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarPlanoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var plano = await _context.Planos.FindAsync(id);
            if (plano == null)
                return NotFound();

            plano.Nome = request.Nome;
            plano.Descricao = request.Descricao;
            plano.Valor = request.Valor;
            plano.LimiteExportacoesMes = request.LimiteExportacoesMes;
            plano.LimiteLeadsPorExportacao = request.LimiteLeadsPorExportacao;
            plano.PermiteExportarEmail = request.PermiteExportarEmail;
            plano.PermiteExportarTelefone = request.PermiteExportarTelefone;
            plano.PermiteFiltrosAvancados = request.PermiteFiltrosAvancados;
            plano.SuportePrioritario = request.SuportePrioritario;
            plano.OrdemExibicao = request.OrdemExibicao;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Excluir(int id)
        {
            var plano = await _context.Planos.FindAsync(id);
            if (plano == null)
                return NotFound();

            // Verificar se há clientes usando este plano
            var clientesUsando = await _context.Clientes.AnyAsync(c => c.PlanoId == id);
            if (clientesUsando)
                return BadRequest(new { message = "Existem clientes usando este plano. Não é possível excluir." });

            plano.Ativo = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("estatisticas")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ObterEstatisticas()
        {
            var estatisticas = await _context.Planos
                .Where(p => p.Ativo)
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    TotalClientes = p.Clientes.Count(c => c.Ativo),
                    ReceitaMensal = p.Clientes.Count(c => c.Ativo) * p.Valor
                })
                .ToListAsync();

            var resumo = new
            {
                TotalPlanos = estatisticas.Count,
                TotalClientes = estatisticas.Sum(e => e.TotalClientes),
                ReceitaMensalTotal = estatisticas.Sum(e => e.ReceitaMensal),
                PlanoMaisPopular = estatisticas.OrderByDescending(e => e.TotalClientes).FirstOrDefault()
            };

            return Ok(new { estatisticas, resumo });
        }
    }

    public class CriarPlanoRequest
    {
        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Valor { get; set; }

        [Required]
        [Range(-1, int.MaxValue, ErrorMessage = "Use -1 para ilimitado ou um valor positivo")]
        public int LimiteExportacoesMes { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "O limite deve ser maior que zero")]
        public int LimiteLeadsPorExportacao { get; set; }

        public bool PermiteExportarEmail { get; set; } = true;
        public bool PermiteExportarTelefone { get; set; } = true;
        public bool PermiteFiltrosAvancados { get; set; } = true;
        public bool SuportePrioritario { get; set; } = false;
        public int OrdemExibicao { get; set; } = 0;
    }

    public class AtualizarPlanoRequest : CriarPlanoRequest
    {
    }
}