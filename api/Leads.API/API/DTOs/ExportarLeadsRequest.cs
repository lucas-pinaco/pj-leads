public class ExportarLeadsRequest
{
    public string? RazaoOuFantasia { get; set; }
    public string? CNAE { get; set; }
    public string? NaturezaJuridica { get; set; }
    public List<string>? SituacoesCadastrais { get; set; }
    public string? Estado { get; set; }
    public string? Municipio { get; set; }
    public string? Bairro { get; set; }
    public string? CEP { get; set; }
    public string? DDD { get; set; }

    public DateTime? DataAberturaDe { get; set; }
    public DateTime? DataAberturaAte { get; set; }

    public decimal? CapitalSocialMinimo { get; set; }
    public decimal? CapitalSocialMaximo { get; set; }

    public bool SomenteMEI { get; set; }
    public bool ExcluirMEI { get; set; }

    public bool SomenteMatriz { get; set; }
    public bool SomenteFilial { get; set; }

    public bool ComTelefone { get; set; }
    public bool SomenteFixo { get; set; }
    public bool SomenteCelular { get; set; }

    public bool ComEmail { get; set; }

    public int Quantidade { get; set; } = 100;
    public bool IncluirDuplicados { get; set; }

    public string EmailDestino { get; set; }
    public string NomeArquivo { get; set; }
}