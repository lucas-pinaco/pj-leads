using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Leads.API.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;

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
        public async Task<IActionResult> Listar()
        {
            var planos = await _context.Planos
                .Where(p => p.Ativo)
                .OrderBy(p => p.OrdemExibicao)
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
                    LimiteExportacoesMesTexto = p.LimiteExportacoesMes == -1 ? "Ilimitado" : p.LimiteExportacoesMes.ToString(),
                    Recursos = new List<string>()
                })
                .ToListAsync();

            // Adicionar recursos em formato de lista
            foreach (var plano in planos)
            {
                var recursos = new List<string>();

                recursos.Add($"{plano.LimiteExportacoesMesTexto} exportações por mês");
                recursos.Add($"Até {plano.LimiteLeadsPorExportacao} leads por exportação");

                if (plano.PermiteExportarEmail)
                    recursos.Add("Exportar e-mails");

                if (plano.PermiteExportarTelefone)
                    recursos.Add("Exportar telefones");

                if (plano.PermiteFiltrosAvancados)
                    recursos.Add("Filtros avançados");

                if (plano.SuportePrioritario)
                    recursos.Add("Suporte prioritário");

                plano.Recursos.AddRange(recursos);
            }

            return Ok(planos);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var plano = await _context.Planos
                .Where(p => p.Id == id && p.Ativo)
                .FirstOrDefaultAsync();

            if (plano == null)
                return NotFound();

            return Ok(plano);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] Plano plano)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Planos.Add(plano);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = plano.Id }, plano);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] Plano plano)
        {
            if (id != plano.Id)
                return BadRequest();

            _context.Entry(plano).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PlanoExists(id))
                    return NotFound();
                throw;
            }

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
            var clientesUsandoPlano = await _context.Clientes
                .AnyAsync(c => c.PlanoId == id);

            if (clientesUsandoPlano)
                return BadRequest(new { message = "Existem clientes usando este plano. Não é possível excluir." });

            plano.Ativo = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> PlanoExists(int id)
        {
            return await _context.Planos.AnyAsync(e => e.Id == id);
        }
    }
}