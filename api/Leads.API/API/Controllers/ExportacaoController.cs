using Leads.API.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportacaoController : ControllerBase
    {
        private readonly ExportacaoService _exportacaoService;
        private readonly EmailService _emailService;

        public ExportacaoController(ExportacaoService exportacaoService, EmailService emailService)
        {
            _exportacaoService = exportacaoService;
            _emailService = emailService;
        }

        [HttpPost("exportar")]
        public async Task<IActionResult> Exportar([FromBody] ExportarLeadsRequest request)
        {
            try
            {
                var arquivo = await _exportacaoService.ExportarLeadsAsync(request);

                return File(arquivo,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    request.NomeArquivo ?? "leads-exportados.xlsx");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao processar exportação", details = ex.Message });
            }
        }
    }
}