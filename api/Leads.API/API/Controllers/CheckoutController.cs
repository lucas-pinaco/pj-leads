using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly CheckoutService _checkoutService;

    public CheckoutController(CheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpPost("criar")]
    public async Task<IActionResult> Criar([FromBody] CriarCheckoutRequest request)
    {
        var checkout = await _checkoutService.CriarCheckoutAsync(request.EmailCliente);
        return Ok(checkout);
    }

    [HttpPost("confirmar/{id}")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var sucesso = await _checkoutService.ConfirmarPagamentoAsync(id);
        if (!sucesso)
            return NotFound("Checkout não encontrado");

        return Ok("Pagamento confirmado");
    }

    [HttpGet("verificar/{id}")]
    public async Task<IActionResult> Verificar(int id)
    {
        var pago = await _checkoutService.VerificarPagamentoAsync(id);
        return Ok(new { Pago = pago });
    }
}