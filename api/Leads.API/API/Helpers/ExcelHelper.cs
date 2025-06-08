using ClosedXML.Excel;

public static class ExcelHelper
{
    public static List<Lead> LerLeadsExcel(Stream stream)
    {
        var leads = new List<Lead>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var lead = new Lead
            {
                DataAbertura = row.Cell(1).GetDateTime(),
                SituacaoCadastral = row.Cell(2).GetString(),
                RazaoSocial = row.Cell(3).GetString(),
                NomeFantasia = row.Cell(4).GetString(),
                CNPJ = row.Cell(5).GetString(),
                CNPJRaiz = row.Cell(6).GetString(),
                AtividadePrincipalCodigo = row.Cell(7).GetString(),
                AtividadePrincipalDescricao = row.Cell(8).GetString(),
                ContatoTelefone = row.Cell(9).GetString(),
                ContatoTelefoneTipo = row.Cell(10).GetString(),
                ContatoEmail = row.Cell(11).GetString(),
                CodigoNaturezaJuridica = row.Cell(12).GetString(),
                DescricaoNaturezaJuridica = row.Cell(13).GetString(),
                Logradouro = row.Cell(14).GetString(),
                Numero = row.Cell(15).GetString(),
                Bairro = row.Cell(16).GetString(),
                Cidade = row.Cell(17).GetString(),
                Estado = row.Cell(18).GetString(),
                CEP = row.Cell(19).GetString(),
                CapitalSocial = row.Cell(20).GetString(),
                QuadroSocietario1 = row.Cell(21).GetString(),
                QuadroSocietario2 = row.Cell(22).GetString(),
                MatrizFilial = row.Cell(23).GetString(),
                MEI = row.Cell(24).GetString(),
                Porte = row.Cell(25).GetString(),

                Duplicado = false,
                Ativo = true
            };

            leads.Add(lead);
        }

        return leads;
    }
}