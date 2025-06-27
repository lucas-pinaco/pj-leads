using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Leads.API.Domain.Entities;

namespace Leads.API.Services
{
    public class CheckoutService
    {
        private readonly AppDbContext _context;

        public CheckoutService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> CriarCheckoutAsync(string emailCliente)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Plano)
                .FirstOrDefaultAsync(c => c.Email == emailCliente);

            if (cliente == null)
                throw new InvalidOperationException("Cliente não encontrado");

            // Aqui você integraria com um gateway de pagamento
            // Por exemplo: Stripe, PagSeguro, etc.

            var checkout = new
            {
                ClienteId = cliente.Id,
                PlanoId = cliente.PlanoId,
                Valor = cliente.Plano.Valor,
                DataCriacao = DateTime.UtcNow,
                Status = "Pendente",
                // URL do gateway de pagamento seria retornada aqui
                UrlPagamento = $"https://pagamento.exemplo.com/checkout/{Guid.NewGuid()}"
            };

            return checkout;
        }

        public async Task<bool> ConfirmarPagamentoAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return false;

            cliente.StatusPagamento = "Pago";
            cliente.DataUltimoPagamento = DateTime.UtcNow;
            cliente.DataVencimento = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerificarPagamentoAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return false;

            return cliente.StatusPagamento == "Pago" &&
                   cliente.DataVencimento > DateTime.UtcNow;
        }
    }
}