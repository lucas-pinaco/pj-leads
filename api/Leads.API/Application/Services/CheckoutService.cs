public class CheckoutService
{
    private readonly AppDbContext _context;

    public CheckoutService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Checkout> CriarCheckoutAsync(string emailCliente)
    {
        var checkout = new Checkout
        {
            EmailCliente = emailCliente,
            Pago = false,
            DataCriacao = DateTime.UtcNow
        };

        _context.Checkouts.Add(checkout);
        await _context.SaveChangesAsync();

        return checkout;
    }

    public async Task<bool> ConfirmarPagamentoAsync(int checkoutId)
    {
        var checkout = await _context.Checkouts.FindAsync(checkoutId);
        if (checkout == null) return false;

        checkout.Pago = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> VerificarPagamentoAsync(int checkoutId)
    {
        var checkout = await _context.Checkouts.FindAsync(checkoutId);
        return checkout?.Pago ?? false;
    }
}