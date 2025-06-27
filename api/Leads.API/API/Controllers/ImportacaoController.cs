using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Leads.API.Domain.Entities;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ADICIONADO - Apenas admin pode importar
    public class ImportacaoController : ControllerBase
    {
        private readonly ImportacaoService _importacaoService;

        public ImportacaoController(ImportacaoService importacaoService)
        {
            _importacaoService = importacaoService;
        }

        [HttpPost("importar")]
        public async Task<IActionResult> Importar([FromForm] ImportarLeadsRequest request)
        {
            if (request.Arquivo == null || request.Arquivo.Length == 0)
                return BadRequest("Arquivo inválido");

            List<Lead> leads = new();

            var ext = Path.GetExtension(request.Arquivo.FileName);

            using var stream = request.Arquivo.OpenReadStream();

            if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                leads = CsvHelperUtil.LerLeadsCsv(stream);
            else if (ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".xls", StringComparison.OrdinalIgnoreCase))
                leads = ExcelHelper.LerLeadsExcel(stream);
            else
                return BadRequest("Formato de arquivo não suportado. Use CSV ou Excel.");

            await _importacaoService.ImportarLeadsAsync(leads);

            return Ok(new { message = "Importação concluída", quantidadeLeads = leads.Count });
        }
    }

    public class ImportarLeadsRequest
    {
        public IFormFile Arquivo { get; set; }
    }
}