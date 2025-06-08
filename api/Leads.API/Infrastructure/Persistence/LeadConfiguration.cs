using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DataAbertura);
        builder.Property(e => e.SituacaoCadastral).HasMaxLength(200);
        builder.Property(e => e.RazaoSocial).HasMaxLength(200);
        builder.Property(e => e.NomeFantasia).HasMaxLength(200);
        builder.Property(e => e.CNPJ).HasMaxLength(50);
        builder.Property(e => e.CNPJRaiz).HasMaxLength(50);
        builder.Property(e => e.AtividadePrincipalCodigo).HasMaxLength(50);
        builder.Property(e => e.AtividadePrincipalDescricao).HasMaxLength(200);
        builder.Property(e => e.ContatoTelefone).HasMaxLength(100);
        builder.Property(e => e.ContatoTelefoneTipo).HasMaxLength(50);
        builder.Property(e => e.ContatoEmail).HasMaxLength(200);
        builder.Property(e => e.CodigoNaturezaJuridica).HasMaxLength(50);
        builder.Property(e => e.DescricaoNaturezaJuridica).HasMaxLength(200);
        builder.Property(e => e.Logradouro).HasMaxLength(200);
        builder.Property(e => e.Numero).HasMaxLength(50);
        builder.Property(e => e.Bairro).HasMaxLength(100);
        builder.Property(e => e.Cidade).HasMaxLength(100);
        builder.Property(e => e.Estado).HasMaxLength(50);
        builder.Property(e => e.CEP).HasMaxLength(20);
        builder.Property(e => e.CapitalSocial).HasMaxLength(50);
        builder.Property(e => e.QuadroSocietario1).HasMaxLength(200);
        builder.Property(e => e.QuadroSocietario2).HasMaxLength(200);
        builder.Property(e => e.MatrizFilial).HasMaxLength(50);
        builder.Property(e => e.MEI).HasMaxLength(50);
        builder.Property(e => e.Porte).HasMaxLength(50);

        builder.Property(e => e.Duplicado);
        builder.Property(e => e.Ativo).HasDefaultValue(true);
    }
}
