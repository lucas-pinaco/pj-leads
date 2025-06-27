using Leads.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listar(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] string busca = null)
        {
            var query = _context.Clientes
                .Include(c => c.Plano)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(c =>
                    c.RazaoSocial.Contains(busca) ||
                    c.NomeFantasia.Contains(busca) ||
                    c.CNPJ.Contains(busca) ||
                    c.Email.Contains(busca));
            }

            var totalItens = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            var clientes = await query
                  .OrderBy(c => c.RazaoSocial)
                  .Skip((pagina - 1) * tamanhoPagina)
                  .Take(tamanhoPagina)
                  .Select(c => new
                  {
                      c.Id,
                      c.RazaoSocial,
                      NomeFantasia = c.NomeFantasia ?? "",
                      c.CNPJ,
                      Email = c.Email ?? "",
                      Telefone = c.Telefone ?? "",
                      c.DataCadastro,
                      c.Ativo,
                      Plano = new
                      {
                          c.Plano.Id,
                          Nome = c.Plano.Nome ?? "",
                          c.Plano.Valor
                      },
                      c.ExportacoesRealizadasMes,
                      StatusPagamento = c.StatusPagamento ?? "",
                      c.DataVencimento,
                      TotalUsuarios = c.Usuarios.Count()
                  })
                  .ToListAsync();

            return Ok(new
            {
                clientes,
                totalItens,
                totalPaginas,
                paginaAtual = pagina
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Plano)
                .Include(c => c.Usuarios)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.RazaoSocial,
                    c.NomeFantasia,
                    c.CNPJ,
                    c.Email,
                    c.Telefone,
                    c.Endereco,
                    c.Cidade,
                    c.Estado,
                    c.CEP,
                    c.DataCadastro,
                    c.Ativo,
                    Plano = new
                    {
                        c.Plano.Id,
                        c.Plano.Nome,
                        c.Plano.Valor,
                        c.Plano.LimiteExportacoesMes,
                        c.Plano.LimiteLeadsPorExportacao
                    },
                    c.ExportacoesRealizadasMes,
                    c.StatusPagamento,
                    c.DataVencimento,
                    c.DataUltimoPagamento,
                    Usuarios = c.Usuarios.Select(u => new
                    {
                        u.Id,
                        u.NomeUsuario,
                        u.Email,
                        u.Perfil,
                        u.Ativo
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar se CNPJ já existe
            if (await _context.Clientes.AnyAsync(c => c.CNPJ == request.CNPJ))
                return BadRequest(new { message = "CNPJ já cadastrado." });

            var cliente = new Cliente
            {
                RazaoSocial = request.RazaoSocial,
                NomeFantasia = request.NomeFantasia,
                CNPJ = request.CNPJ,
                Email = request.Email,
                Telefone = request.Telefone,
                Endereco = request.Endereco,
                Cidade = request.Cidade,
                Estado = request.Estado,
                CEP = request.CEP,
                PlanoId = request.PlanoId,
                DataVencimento = DateTime.UtcNow.AddDays(30) // 30 dias de trial
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarClienteRequest request)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            // Verificar se CNPJ já existe em outro cliente
            if (await _context.Clientes.AnyAsync(c => c.CNPJ == request.CNPJ && c.Id != id))
                return BadRequest(new { message = "CNPJ já cadastrado para outro cliente." });

            cliente.RazaoSocial = request.RazaoSocial;
            cliente.NomeFantasia = request.NomeFantasia;
            cliente.CNPJ = request.CNPJ;
            cliente.Email = request.Email;
            cliente.Telefone = request.Telefone;
            cliente.Endereco = request.Endereco;
            cliente.Cidade = request.Cidade;
            cliente.Estado = request.Estado;
            cliente.CEP = request.CEP;
            cliente.PlanoId = request.PlanoId;
            cliente.Ativo = request.Ativo;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/alterar-plano")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AlterarPlano(int id, [FromBody] AlterarPlanoRequest request)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            var plano = await _context.Planos.FindAsync(request.NovoPlanoId);
            if (plano == null)
                return BadRequest(new { message = "Plano não encontrado." });

            cliente.PlanoId = request.NovoPlanoId;
            cliente.ExportacoesRealizadasMes = 0; // Reset do contador
            cliente.UltimaResetMensal = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Plano alterado com sucesso." });
        }

        [HttpGet("meu-perfil")]
        public async Task<IActionResult> MeuPerfil()
        {
            // Pegar o usuário logado do token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .ThenInclude(c => c.Plano)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado." });

            // Se for admin, retornar informações especiais
            if (usuario.Perfil == "Admin")
            {
                return Ok(new
                {
                    Cliente = new
                    {
                        Id = 0,
                        RazaoSocial = "Administrador",
                        NomeFantasia = "Admin",
                        CNPJ = "00000000000000",
                        Email = usuario.Email,
                        StatusPagamento = "Admin",
                        DataVencimento = (DateTime?)null
                    },
                    Plano = new
                    {
                        Id = 0,
                        Nome = "Administrador",
                        LimiteExportacoesMes = -1,
                        LimiteLeadsPorExportacao = int.MaxValue,
                        ExportacoesUtilizadas = 0,
                        ExportacoesDisponiveis = "Ilimitado"
                    },
                    IsAdmin = true
                });
            }

            if (usuario.Cliente == null)
                return NotFound(new { message = "Cliente não encontrado para este usuário." });

            var exportacoesDoMes = await _context.HistoricoExportacoes
                .Where(h => h.ClienteId == usuario.ClienteId.Value &&
                           h.DataExportacao.Month == DateTime.UtcNow.Month &&
                           h.DataExportacao.Year == DateTime.UtcNow.Year)
                .CountAsync();

            return Ok(new
            {
                Cliente = new
                {
                    usuario.Cliente.Id,
                    usuario.Cliente.RazaoSocial,
                    usuario.Cliente.NomeFantasia,
                    usuario.Cliente.CNPJ,
                    usuario.Cliente.Email,
                    usuario.Cliente.StatusPagamento,
                    usuario.Cliente.DataVencimento
                },
                Plano = new
                {
                    usuario.Cliente.Plano.Id,
                    usuario.Cliente.Plano.Nome,
                    usuario.Cliente.Plano.LimiteExportacoesMes,
                    usuario.Cliente.Plano.LimiteLeadsPorExportacao,
                    ExportacoesUtilizadas = exportacoesDoMes,
                    ExportacoesDisponiveis = usuario.Cliente.Plano.LimiteExportacoesMes == -1
                        ? "Ilimitado"
                        : (usuario.Cliente.Plano.LimiteExportacoesMes - exportacoesDoMes).ToString()
                },
                IsAdmin = false
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Excluir(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Usuarios)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return NotFound();

            if (cliente.Usuarios.Any())
                return BadRequest(new { message = "Cliente possui usuários vinculados. Remova os usuários antes de excluir." });

            cliente.Ativo = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CriarClienteRequest
    {
        public string RazaoSocial { get; set; }
        public string NomeFantasia { get; set; }
        public string CNPJ { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string CEP { get; set; }
        public int PlanoId { get; set; }
    }

    public class AtualizarClienteRequest : CriarClienteRequest
    {
        public bool Ativo { get; set; }
    }

    public class AlterarPlanoRequest
    {
        public int NovoPlanoId { get; set; }
    }
}