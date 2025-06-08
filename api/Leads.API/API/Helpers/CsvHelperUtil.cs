using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

public static class CsvHelperUtil
{
    public static List<Lead> LerLeadsCsv(Stream stream)
    {
        var leads = new List<Lead>();

        using var reader = new StreamReader(stream);

        string firstLine = reader.ReadLine()!;
        var delimiter = firstLine.Contains(';') ? ";" : ",";

        var config = new CsvConfiguration(new CultureInfo("pt-BR")) 
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            Delimiter = delimiter
        };

        using var csv = new CsvReader(reader, config);

        while (csv.Read())
        {
            var lead = new Lead
            {
                DataAbertura = csv.GetField<DateTime?>(0),
                SituacaoCadastral = csv.GetField<string>(1),
                RazaoSocial = csv.GetField<string>(2),
                NomeFantasia = csv.GetField<string>(3),
                CNPJ = csv.GetField<string>(4),
                CNPJRaiz = csv.GetField<string>(5),
                AtividadePrincipalCodigo = csv.GetField<string>(6),
                AtividadePrincipalDescricao = csv.GetField<string>(7),
                ContatoTelefone = csv.GetField<string>(8),
                ContatoTelefoneTipo = csv.GetField<string>(9),
                ContatoEmail = csv.GetField<string>(10),
                CodigoNaturezaJuridica = csv.GetField<string>(11),
                DescricaoNaturezaJuridica = csv.GetField<string>(12),
                Logradouro = csv.GetField<string>(13),
                Numero = csv.GetField<string>(14),
                Bairro = csv.GetField<string>(15),
                Cidade = csv.GetField<string>(16),
                Estado = csv.GetField<string>(17),
                CEP = csv.GetField<string>(18),
                CapitalSocial = csv.GetField<string>(19),
                QuadroSocietario1 = csv.GetField<string>(20),
                QuadroSocietario2 = csv.GetField<string>(21),
                MatrizFilial = csv.GetField<string>(22),
                MEI = csv.GetField<string>(23),
                Porte = csv.GetField<string>(24),

                Duplicado = false,
                Ativo = true
            };

            leads.Add(lead);
        }

        return leads;
    }
}
