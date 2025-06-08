public class Lead
{
    public int Id { get; set; }
    public DateTime? DataAbertura { get; set; }
    public string SituacaoCadastral { get; set; }
    public string RazaoSocial { get; set; }
    public string NomeFantasia { get; set; }
    public string CNPJ { get; set; }
    public string CNPJRaiz { get; set; }
    public string AtividadePrincipalCodigo { get; set; }
    public string AtividadePrincipalDescricao { get; set; }
    public string ContatoTelefone { get; set; }
    public string ContatoTelefoneTipo { get; set; }
    public string ContatoEmail { get; set; }
    public string CodigoNaturezaJuridica { get; set; }
    public string DescricaoNaturezaJuridica { get; set; }
    public string Logradouro { get; set; }
    public string Numero { get; set; }
    public string Bairro { get; set; }
    public string Cidade { get; set; }
    public string Estado { get; set; }
    public string CEP { get; set; }
    public string CapitalSocial { get; set; }
    public string QuadroSocietario1 { get; set; }
    public string QuadroSocietario2 { get; set; }
    public string MatrizFilial { get; set; }
    public string MEI { get; set; }
    public string Porte { get; set; }

    public bool Duplicado { get; set; }
    public bool Ativo { get; set; }
}