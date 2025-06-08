using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ExportacaoController : ControllerBase
{
    private readonly ExportacaoService _exportacaoService;
    private readonly EmailService _emailService;

    public ExportacaoController(ExportacaoService exportacaoService, EmailService emailService)
    {
        _exportacaoService = exportacaoService;
        _emailService = emailService;
    }

    //[HttpPost("exportar")]
    //public async Task<IActionResult> Exportar([FromBody] ExportarLeadsRequest request)
    //{
    //    var arquivo = await _exportacaoService.ExportarLeadsAsync(request.IncluirDuplicados, request.Quantidade);

    //    await _emailService.EnviarArquivoPorEmailAsync(request.EmailDestino, arquivo, request.NomeArquivo);

    //    return Ok("Arquivo exportado e enviado com sucesso");
    //}

    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] ExportarLeadsRequest request)
    {
        var arquivo = await _exportacaoService.ExportarLeadsAsync(request.IncluirDuplicados, request.Quantidade);

        return File(arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            request.NomeArquivo ?? "leads-exportados.xlsx");
    }
}