using ClosedXML.Excel;
using Leads.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

public class ExportacaoService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExportacaoService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<byte[]> ExportarLeadsAsync(ExportarLeadsRequest request)
    {
        var (userId, clienteId) = await ObterUsuarioAtual();

        // Verificar limites
        var (permitido, mensagem, limiteDisponivel) = await VerificarLimiteExportacao(clienteId, request.Quantidade);
        if (!permitido)
            throw new InvalidOperationException(mensagem);

        var cliente = await _context.Clientes
            .Include(c => c.Plano)
            .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

        var query = _context.Leads.AsQueryable();

        // Aplicar filtros (código existente)
        if (!request.IncluirDuplicados)
            query = query.Where(x => !x.Duplicado);

        if (!string.IsNullOrEmpty(request.RazaoOuFantasia))
            query = query.Where(x =>
                x.RazaoSocial.Contains(request.RazaoOuFantasia) ||
                x.NomeFantasia.Contains(request.RazaoOuFantasia));

        if (!string.IsNullOrEmpty(request.CNAE))
            query = query.Where(x => x.AtividadePrincipalCodigo.Contains(request.CNAE) || x.AtividadePrincipalDescricao.Contains(request.CNAE));

        if (!string.IsNullOrEmpty(request.NaturezaJuridica))
            query = query.Where(x => x.DescricaoNaturezaJuridica.Contains(request.NaturezaJuridica));

        if (request.SituacoesCadastrais?.Any() == true)
            query = query.Where(x => request.SituacoesCadastrais.Contains(x.SituacaoCadastral));

        if (!string.IsNullOrEmpty(request.Estado))
            query = query.Where(x => x.Estado == request.Estado);

        if (!string.IsNullOrEmpty(request.Municipio))
            query = query.Where(x => x.Cidade.Contains(request.Municipio));

        if (!string.IsNullOrEmpty(request.Bairro))
            query = query.Where(x => x.Bairro.Contains(request.Bairro));

        if (!string.IsNullOrEmpty(request.CEP))
            query = query.Where(x => x.CEP == request.CEP);

        if (!string.IsNullOrEmpty(request.DDD))
            query = query.Where(x => x.ContatoTelefone.StartsWith(request.DDD));

        if (request.DataAberturaDe.HasValue)
            query = query.Where(x => x.DataAbertura >= request.DataAberturaDe);

        if (request.DataAberturaAte.HasValue)
            query = query.Where(x => x.DataAbertura <= request.DataAberturaAte);

        if (request.CapitalSocialMinimo.HasValue)
            query = query.Where(x => Convert.ToDecimal(x.CapitalSocial) >= request.CapitalSocialMinimo);

        if (request.CapitalSocialMaximo.HasValue)
            query = query.Where(x => Convert.ToDecimal(x.CapitalSocial) <= request.CapitalSocialMaximo);

        if (request.SomenteMEI)
            query = query.Where(x => x.MEI == "VERDADEIRO");

        if (request.ExcluirMEI)
            query = query.Where(x => x.MEI != "VERDADEIRO");

        if (request.SomenteMatriz)
            query = query.Where(x => x.MatrizFilial == "Matriz");

        if (request.SomenteFilial)
            query = query.Where(x => x.MatrizFilial == "Filial");

        if (request.ComTelefone)
            query = query.Where(x => !string.IsNullOrEmpty(x.ContatoTelefone));

        if (request.SomenteFixo)
            query = query.Where(x => x.ContatoTelefoneTipo == "fixo");

        if (request.SomenteCelular)
            query = query.Where(x => x.ContatoTelefoneTipo == "celular");

        if (request.ComEmail)
            query = query.Where(x => !string.IsNullOrEmpty(x.ContatoEmail));

        var leads = await query.Take(request.Quantidade).ToListAsync();

        // aqui você chama o ExcelHelper com os leads
        //return ExcelHelper.GerarExcel(leads);

        //var leadsParaExportar = await query.Take(20).ToListAsync();

        var quantidadeMaxima = Math.Min(request.Quantidade, cliente.Plano.LimiteLeadsPorExportacao);
        var leadsParaExportar = await query.Take(quantidadeMaxima).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Leads");

        // Cabeçalho
        worksheet.Cell(1, 1).Value = "Data Abertura";
        worksheet.Cell(1, 2).Value = "Situação Cadastral";
        worksheet.Cell(1, 3).Value = "Razão Social";
        worksheet.Cell(1, 4).Value = "Nome Fantasia";
        worksheet.Cell(1, 5).Value = "CNPJ";
        worksheet.Cell(1, 6).Value = "CNPJ Raiz";
        worksheet.Cell(1, 7).Value = "Atividade Principal Código";
        worksheet.Cell(1, 8).Value = "Atividade Principal Descrição";
        worksheet.Cell(1, 9).Value = "Telefone";
        worksheet.Cell(1, 10).Value = "Tipo Telefone";
        worksheet.Cell(1, 11).Value = "Email";
        worksheet.Cell(1, 12).Value = "Código Natureza Jurídica";
        worksheet.Cell(1, 13).Value = "Descrição Natureza Jurídica";
        worksheet.Cell(1, 14).Value = "Logradouro";
        worksheet.Cell(1, 15).Value = "Número";
        worksheet.Cell(1, 16).Value = "Bairro";
        worksheet.Cell(1, 17).Value = "Cidade";
        worksheet.Cell(1, 18).Value = "Estado";
        worksheet.Cell(1, 19).Value = "CEP";
        worksheet.Cell(1, 20).Value = "Capital Social";
        worksheet.Cell(1, 21).Value = "Quadro Societário 1";
        worksheet.Cell(1, 22).Value = "Quadro Societário 2";
        worksheet.Cell(1, 23).Value = "Matriz/Filial";
        worksheet.Cell(1, 24).Value = "MEI";
        worksheet.Cell(1, 25).Value = "Porte";
        worksheet.Cell(1, 26).Value = "Duplicado";
        worksheet.Cell(1, 27).Value = "Ativo";

        for (int i = 0; i < leadsParaExportar.Count; i++)
        {
            var lead = leadsParaExportar[i];
            worksheet.Cell(i + 2, 1).Value = lead.DataAbertura?.ToString("dd/MM/yyyy");
            worksheet.Cell(i + 2, 2).Value = lead.SituacaoCadastral;
            worksheet.Cell(i + 2, 3).Value = lead.RazaoSocial;
            worksheet.Cell(i + 2, 4).Value = lead.NomeFantasia;
            worksheet.Cell(i + 2, 5).Value = lead.CNPJ;
            worksheet.Cell(i + 2, 6).Value = lead.CNPJRaiz;
            worksheet.Cell(i + 2, 7).Value = lead.AtividadePrincipalCodigo;
            worksheet.Cell(i + 2, 8).Value = lead.AtividadePrincipalDescricao;
            worksheet.Cell(i + 2, 9).Value = lead.ContatoTelefone;
            worksheet.Cell(i + 2, 10).Value = lead.ContatoTelefoneTipo;
            worksheet.Cell(i + 2, 11).Value = lead.ContatoEmail;
            worksheet.Cell(i + 2, 12).Value = lead.CodigoNaturezaJuridica;
            worksheet.Cell(i + 2, 13).Value = lead.DescricaoNaturezaJuridica;
            worksheet.Cell(i + 2, 14).Value = lead.Logradouro;
            worksheet.Cell(i + 2, 15).Value = lead.Numero;
            worksheet.Cell(i + 2, 16).Value = lead.Bairro;
            worksheet.Cell(i + 2, 17).Value = lead.Cidade;
            worksheet.Cell(i + 2, 18).Value = lead.Estado;
            worksheet.Cell(i + 2, 19).Value = lead.CEP;
            worksheet.Cell(i + 2, 20).Value = lead.CapitalSocial;
            worksheet.Cell(i + 2, 21).Value = lead.QuadroSocietario1;
            worksheet.Cell(i + 2, 22).Value = lead.QuadroSocietario2;
            worksheet.Cell(i + 2, 23).Value = lead.MatrizFilial;
            worksheet.Cell(i + 2, 24).Value = lead.MEI;
            worksheet.Cell(i + 2, 25).Value = lead.Porte;
            worksheet.Cell(i + 2, 26).Value = lead.Duplicado ? "Sim" : "Não";
            worksheet.Cell(i + 2, 27).Value = lead.Ativo ? "Sim" : "Não";
        }


        var historico = new HistoricoExportacao
        {
            ClienteId = clienteId.Value,
            UsuarioId = userId,
            QuantidadeLeads = leads.Count,
            FiltrosUtilizados = JsonConvert.SerializeObject(request),
            NomeArquivo = request.NomeArquivo ?? "leads-exportados.xlsx",
            EmailDestino = request.EmailDestino,
            EnviadoPorEmail = !string.IsNullOrEmpty(request.EmailDestino),
            PlanoNome = cliente.Plano.Nome,
            LimiteDisponivel = limiteDisponivel - 1
        };

        _context.HistoricoExportacoes.Add(historico);

        // Atualizar contador do cliente
        cliente.ExportacoesRealizadasMes++;

        await _context.SaveChangesAsync();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    //public async Task<byte[]> ExportarLeadsAsync(bool incluirDuplicados, int quantidade)
    //{
    //    var query = _context.Leads.AsQueryable();

    //    if (!incluirDuplicados)
    //        query = query.Where(x => !x.Duplicado);

    //    var leadsParaExportar = await query.Take(quantidade).ToListAsync();

    //    using var workbook = new XLWorkbook();
    //    var worksheet = workbook.Worksheets.Add("Leads");

    //    // Cabeçalho
    //    worksheet.Cell(1, 1).Value = "Data Abertura";
    //    worksheet.Cell(1, 2).Value = "Situação Cadastral";
    //    worksheet.Cell(1, 3).Value = "Razão Social";
    //    worksheet.Cell(1, 4).Value = "Nome Fantasia";
    //    worksheet.Cell(1, 5).Value = "CNPJ";
    //    worksheet.Cell(1, 6).Value = "CNPJ Raiz";
    //    worksheet.Cell(1, 7).Value = "Atividade Principal Código";
    //    worksheet.Cell(1, 8).Value = "Atividade Principal Descrição";
    //    worksheet.Cell(1, 9).Value = "Telefone";
    //    worksheet.Cell(1, 10).Value = "Tipo Telefone";
    //    worksheet.Cell(1, 11).Value = "Email";
    //    worksheet.Cell(1, 12).Value = "Código Natureza Jurídica";
    //    worksheet.Cell(1, 13).Value = "Descrição Natureza Jurídica";
    //    worksheet.Cell(1, 14).Value = "Logradouro";
    //    worksheet.Cell(1, 15).Value = "Número";
    //    worksheet.Cell(1, 16).Value = "Bairro";
    //    worksheet.Cell(1, 17).Value = "Cidade";
    //    worksheet.Cell(1, 18).Value = "Estado";
    //    worksheet.Cell(1, 19).Value = "CEP";
    //    worksheet.Cell(1, 20).Value = "Capital Social";
    //    worksheet.Cell(1, 21).Value = "Quadro Societário 1";
    //    worksheet.Cell(1, 22).Value = "Quadro Societário 2";
    //    worksheet.Cell(1, 23).Value = "Matriz/Filial";
    //    worksheet.Cell(1, 24).Value = "MEI";
    //    worksheet.Cell(1, 25).Value = "Porte";
    //    worksheet.Cell(1, 26).Value = "Duplicado";
    //    worksheet.Cell(1, 27).Value = "Ativo";

    //    for (int i = 0; i < leadsParaExportar.Count; i++)
    //    {
    //        var lead = leadsParaExportar[i];
    //        worksheet.Cell(i + 2, 1).Value = lead.DataAbertura?.ToString("dd/MM/yyyy");
    //        worksheet.Cell(i + 2, 2).Value = lead.SituacaoCadastral;
    //        worksheet.Cell(i + 2, 3).Value = lead.RazaoSocial;
    //        worksheet.Cell(i + 2, 4).Value = lead.NomeFantasia;
    //        worksheet.Cell(i + 2, 5).Value = lead.CNPJ;
    //        worksheet.Cell(i + 2, 6).Value = lead.CNPJRaiz;
    //        worksheet.Cell(i + 2, 7).Value = lead.AtividadePrincipalCodigo;
    //        worksheet.Cell(i + 2, 8).Value = lead.AtividadePrincipalDescricao;
    //        worksheet.Cell(i + 2, 9).Value = lead.ContatoTelefone;
    //        worksheet.Cell(i + 2, 10).Value = lead.ContatoTelefoneTipo;
    //        worksheet.Cell(i + 2, 11).Value = lead.ContatoEmail;
    //        worksheet.Cell(i + 2, 12).Value = lead.CodigoNaturezaJuridica;
    //        worksheet.Cell(i + 2, 13).Value = lead.DescricaoNaturezaJuridica;
    //        worksheet.Cell(i + 2, 14).Value = lead.Logradouro;
    //        worksheet.Cell(i + 2, 15).Value = lead.Numero;
    //        worksheet.Cell(i + 2, 16).Value = lead.Bairro;
    //        worksheet.Cell(i + 2, 17).Value = lead.Cidade;
    //        worksheet.Cell(i + 2, 18).Value = lead.Estado;
    //        worksheet.Cell(i + 2, 19).Value = lead.CEP;
    //        worksheet.Cell(i + 2, 20).Value = lead.CapitalSocial;
    //        worksheet.Cell(i + 2, 21).Value = lead.QuadroSocietario1;
    //        worksheet.Cell(i + 2, 22).Value = lead.QuadroSocietario2;
    //        worksheet.Cell(i + 2, 23).Value = lead.MatrizFilial;
    //        worksheet.Cell(i + 2, 24).Value = lead.MEI;
    //        worksheet.Cell(i + 2, 25).Value = lead.Porte;
    //        worksheet.Cell(i + 2, 26).Value = lead.Duplicado ? "Sim" : "Não";
    //        worksheet.Cell(i + 2, 27).Value = lead.Ativo ? "Sim" : "Não";
    //    }

    //    using var stream = new MemoryStream();
    //    workbook.SaveAs(stream);
    //    return stream.ToArray();
    //}

    public async Task<byte[]> ExportarLeadsAsync(bool incluirDuplicados, int quantidade)
    {
        var request = new ExportarLeadsRequest
        {
            IncluirDuplicados = incluirDuplicados,
            Quantidade = quantidade
        };

        return await ExportarLeadsAsync(request);
    }

    private async Task<(int userId, int? clienteId)> ObterUsuarioAtual()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Usuário não identificado");

        var usuario = await _context.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return (userId, usuario?.ClienteId);
    }

    public async Task<(bool permitido, string mensagem, int limiteDisponivel)> VerificarLimiteExportacao(int? clienteId, int quantidadeSolicitada)
    {
        if (!clienteId.HasValue)
            return (false, "Cliente não encontrado", 0);

        var cliente = await _context.Clientes
            .Include(c => c.Plano)
            .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

        if (cliente == null)
            return (false, "Cliente não encontrado", 0);

        // Verificar se precisa resetar o contador mensal
        if (cliente.UltimaResetMensal == null ||
            cliente.UltimaResetMensal.Value.Month != DateTime.UtcNow.Month ||
            cliente.UltimaResetMensal.Value.Year != DateTime.UtcNow.Year)
        {
            cliente.ExportacoesRealizadasMes = 0;
            cliente.UltimaResetMensal = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Verificar limite mensal
        if (cliente.Plano.LimiteExportacoesMes != -1 &&
            cliente.ExportacoesRealizadasMes >= cliente.Plano.LimiteExportacoesMes)
        {
            return (false, "Limite mensal de exportações atingido", 0);
        }

        // Verificar limite por exportação
        if (quantidadeSolicitada > cliente.Plano.LimiteLeadsPorExportacao)
        {
            return (false, $"Quantidade solicitada excede o limite de {cliente.Plano.LimiteLeadsPorExportacao} leads por exportação",
                    cliente.Plano.LimiteLeadsPorExportacao);
        }

        var limiteDisponivel = cliente.Plano.LimiteExportacoesMes == -1
            ? int.MaxValue
            : cliente.Plano.LimiteExportacoesMes - cliente.ExportacoesRealizadasMes;

        return (true, "OK", limiteDisponivel);
    }


    private byte[] GerarArquivoExcel(List<Lead> leads, Plano plano)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Leads");

        // Cabeçalho
        var coluna = 1;
        worksheet.Cell(1, coluna++).Value = "Data Abertura";
        worksheet.Cell(1, coluna++).Value = "Situação Cadastral";
        worksheet.Cell(1, coluna++).Value = "Razão Social";
        worksheet.Cell(1, coluna++).Value = "Nome Fantasia";
        worksheet.Cell(1, coluna++).Value = "CNPJ";

        if (plano.PermiteExportarTelefone)
        {
            worksheet.Cell(1, coluna++).Value = "Telefone";
            worksheet.Cell(1, coluna++).Value = "Tipo Telefone";
        }

        if (plano.PermiteExportarEmail)
        {
            worksheet.Cell(1, coluna++).Value = "Email";
        }

        worksheet.Cell(1, coluna++).Value = "Cidade";
        worksheet.Cell(1, coluna++).Value = "Estado";

        // Dados
        for (int i = 0; i < leads.Count; i++)
        {
            var lead = leads[i];
            coluna = 1;

            worksheet.Cell(i + 2, coluna++).Value = lead.DataAbertura?.ToString("dd/MM/yyyy");
            worksheet.Cell(i + 2, coluna++).Value = lead.SituacaoCadastral;
            worksheet.Cell(i + 2, coluna++).Value = lead.RazaoSocial;
            worksheet.Cell(i + 2, coluna++).Value = lead.NomeFantasia;
            worksheet.Cell(i + 2, coluna++).Value = lead.CNPJ;

            if (plano.PermiteExportarTelefone)
            {
                worksheet.Cell(i + 2, coluna++).Value = lead.ContatoTelefone;
                worksheet.Cell(i + 2, coluna++).Value = lead.ContatoTelefoneTipo;
            }

            if (plano.PermiteExportarEmail)
            {
                worksheet.Cell(i + 2, coluna++).Value = lead.ContatoEmail;
            }

            worksheet.Cell(i + 2, coluna++).Value = lead.Cidade;
            worksheet.Cell(i + 2, coluna++).Value = lead.Estado;
        }

        // Formatar como tabela
        var range = worksheet.Range(1, 1, leads.Count + 1, coluna - 1);
        var table = range.CreateTable();
        table.ShowTotalsRow = false;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
