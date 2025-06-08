using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
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
        else
            leads = ExcelHelper.LerLeadsExcel(stream);

        await _importacaoService.ImportarLeadsAsync(leads);

        return Ok("Importação concluída");
    }
}
