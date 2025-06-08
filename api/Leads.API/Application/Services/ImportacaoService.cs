public class ImportacaoService
{
    private readonly AppDbContext _context;

    public ImportacaoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task ImportarLeadsAsync(IEnumerable<Lead> leads)
    {
        foreach (var lead in leads)
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(lead.ContatoEmail) || string.IsNullOrWhiteSpace(lead.ContatoTelefone))
                continue;

            var duplicado = _context.Leads.Any(x => x.ContatoEmail == lead.ContatoEmail && x.ContatoTelefone == lead.ContatoTelefone);
            lead.Duplicado = duplicado;
            lead.Ativo = !duplicado;

            _context.Leads.Add(lead);
        }

        await _context.SaveChangesAsync();
    }
}