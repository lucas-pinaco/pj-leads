using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leads.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Checkouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pago = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataAbertura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SituacaoCadastral = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CNPJ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CNPJRaiz = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AtividadePrincipalCodigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AtividadePrincipalDescricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContatoTelefone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContatoTelefoneTipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContatoEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodigoNaturezaJuridica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescricaoNaturezaJuridica = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CEP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapitalSocial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuadroSocietario1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuadroSocietario2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MatrizFilial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MEI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Porte = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Duplicado = table.Column<bool>(type: "bit", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Checkouts");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
