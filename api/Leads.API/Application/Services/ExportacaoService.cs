using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Leads.API.Domain.DTOs;
using Leads.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class ExportacaoService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExportacaoService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task<(int userId, int? clienteId)> ObterUsuarioAtual()
    {
        var user = _httpContextAccessor.HttpContext?.User
           ?? throw new UnauthorizedAccessException("Usuário não autenticado");

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Usuário não identificado");

        var usuario = await _context.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return (userId, usuario?.ClienteId);
    }

    public async Task<(bool permitido, string mensagem, int limiteDisponivel)> VerificarLimiteExportacao(int? clienteId, int quantidadeSolicitada)
    {
        // Admin não tem limites
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario?.Perfil == "Admin")
            {
                return (true, "OK", int.MaxValue);
            }
        }

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
    public async Task<byte[]> ExportarLeadsAsync(ExportarLeadsRequest request)
    {
        try
        {
            var (userId, clienteId) = await ObterUsuarioAtual();

            // Verificar se é admin
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            var isAdmin = usuario?.Perfil == "Admin";

            // Verificar limites (admin pula esta verificação)
            if (!isAdmin)
            {
                var (permitido, mensagem, limiteDisponivelCliente) = await VerificarLimiteExportacao(clienteId, request.Quantidade);
                if (!permitido)
                    throw new InvalidOperationException(mensagem);
            }

            // Buscar cliente e plano (pode ser null para admin)
            var cliente = clienteId.HasValue
                ? await _context.Clientes
                    .Include(c => c.Plano)
                    .FirstOrDefaultAsync(c => c.Id == clienteId.Value)
                : null;

            var query = _context.Leads.AsQueryable();

            // Aplicar filtros
            if (!request.IncluirDuplicados)
                query = query.Where(x => !x.Duplicado);

            if (!string.IsNullOrEmpty(request.RazaoOuFantasia))
                query = query.Where(x =>
                    x.RazaoSocial.Contains(request.RazaoOuFantasia) ||
                    x.NomeFantasia.Contains(request.RazaoOuFantasia));

            if (!string.IsNullOrEmpty(request.CNAE))
                query = query.Where(x =>
                    x.AtividadePrincipalCodigo.Contains(request.CNAE) ||
                    x.AtividadePrincipalDescricao.Contains(request.CNAE));

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

            // Determinar quantidade máxima (admin não tem limite)
            var quantidadeMaxima = isAdmin
                ? request.Quantidade
                : Math.Min(request.Quantidade, cliente?.Plano?.LimiteLeadsPorExportacao ?? 100);

            var leadsParaExportar = await query.Take(quantidadeMaxima).ToListAsync();

            // Gerar arquivo Excel
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Leads");

            // Configurar cabeçalho dinamicamente baseado no plano
            var colunas = new List<(string Nome, Func<Lead, object> Valor)>
    {
        ("Data Abertura", l => l.DataAbertura?.ToString("dd/MM/yyyy")),
        ("Situação Cadastral", l => l.SituacaoCadastral),
        ("Razão Social", l => l.RazaoSocial),
        ("Nome Fantasia", l => l.NomeFantasia),
        ("CNPJ", l => l.CNPJ),
        ("CNPJ Raiz", l => l.CNPJRaiz),
        ("Atividade Principal Código", l => l.AtividadePrincipalCodigo),
        ("Atividade Principal Descrição", l => l.AtividadePrincipalDescricao)
    };

            // Adicionar telefone se permitido pelo plano (admin sempre pode)
            if (isAdmin || cliente?.Plano?.PermiteExportarTelefone == true)
            {
                colunas.Add(("Telefone", l => l.ContatoTelefone));
                colunas.Add(("Tipo Telefone", l => l.ContatoTelefoneTipo));
            }

            // Adicionar email se permitido pelo plano (admin sempre pode)
            if (isAdmin || cliente?.Plano?.PermiteExportarEmail == true)
            {
                colunas.Add(("Email", l => l.ContatoEmail));
            }

            // Adicionar demais colunas
            colunas.AddRange(new List<(string Nome, Func<Lead, object> Valor)>
        {
        ("Código Natureza Jurídica", l => l.CodigoNaturezaJuridica),
        ("Descrição Natureza Jurídica", l => l.DescricaoNaturezaJuridica),
        ("Logradouro", l => l.Logradouro),
        ("Número", l => l.Numero),
        ("Bairro", l => l.Bairro),
        ("Cidade", l => l.Cidade),
        ("Estado", l => l.Estado),
        ("CEP", l => l.CEP),
        ("Capital Social", l => l.CapitalSocial),
        ("Quadro Societário 1", l => l.QuadroSocietario1),
        ("Quadro Societário 2", l => l.QuadroSocietario2),
        ("Matriz/Filial", l => l.MatrizFilial),
        ("MEI", l => l.MEI),
        ("Porte", l => l.Porte)
    });

            // Adicionar colunas de controle apenas para admin
            if (isAdmin)
            {
                colunas.Add(("Duplicado", l => l.Duplicado ? "Sim" : "Não"));
                colunas.Add(("Ativo", l => l.Ativo ? "Sim" : "Não"));
            }

            // Escrever cabeçalho
            for (int col = 0; col < colunas.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = colunas[col].Nome;
            }

            // Escrever dados
            for (int row = 0; row < leadsParaExportar.Count; row++)
            {
                var lead = leadsParaExportar[row];
                for (int col = 0; col < colunas.Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = colunas[col].Valor(lead)?.ToString() ?? "";
                }
            }

            // Formatar como tabela
            if (leadsParaExportar.Count > 0)
            {
                var range = worksheet.Range(1, 1, leadsParaExportar.Count + 1, colunas.Count);
                var table = range.CreateTable();
                table.ShowTotalsRow = false;
                table.Theme = XLTableTheme.TableStyleMedium2;

                // Ajustar largura das colunas
                worksheet.Columns().AdjustToContents();
            }

            // Calcular limite disponível
            var limiteDisponivel = isAdmin
                ? int.MaxValue
                : (cliente?.Plano?.LimiteExportacoesMes == -1
                    ? int.MaxValue
                    : cliente.Plano.LimiteExportacoesMes - cliente.ExportacoesRealizadasMes);

            // Registrar no histórico
            var historico = new HistoricoExportacao
            {
                ClienteId = clienteId ?? 0, // 0 para admin sem cliente
                UsuarioId = userId,
                QuantidadeLeads = leadsParaExportar.Count,
                FiltrosUtilizados = JsonConvert.SerializeObject(request),
                NomeArquivo = request.NomeArquivo ?? "leads-exportados.xlsx",
                EmailDestino = request.EmailDestino,
                EnviadoPorEmail = !string.IsNullOrEmpty(request.EmailDestino),
                PlanoNome = isAdmin ? "Administrador" : (cliente?.Plano?.Nome ?? "Desconhecido"),
                LimiteDisponivel = limiteDisponivel
            };

            _context.HistoricoExportacoes.Add(historico);

            // Atualizar contador do cliente (não para admin)
            if (!isAdmin && cliente != null)
            {
                cliente.ExportacoesRealizadasMes++;
            }

            await _context.SaveChangesAsync();

            // Enviar por email se solicitado
            if (!string.IsNullOrEmpty(request.EmailDestino))
            {
                try
                {
                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    var arquivo = stream.ToArray();

                    //await _emailService.EnviarArquivoPorEmailAsync(
                    //    request.EmailDestino,
                    //    arquivo,
                    //    request.NomeArquivo ?? "leads-exportados.xlsx"
                    //);
                }
                catch (Exception ex)
                {
                    // Log do erro mas não falha a operação
                    Console.WriteLine($"Erro ao enviar email: {ex.Message}");
                }
            }

            // Retornar arquivo
            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            throw;
        }
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
        table.Theme = XLTableTheme.TableStyleMedium2;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Método simplificado mantido para compatibilidade
    public async Task<byte[]> ExportarLeadsAsync(bool incluirDuplicados, int quantidade)
    {
        var request = new ExportarLeadsRequest
        {
            IncluirDuplicados = incluirDuplicados,
            Quantidade = quantidade
        };

        return await ExportarLeadsAsync(request);
    }
}