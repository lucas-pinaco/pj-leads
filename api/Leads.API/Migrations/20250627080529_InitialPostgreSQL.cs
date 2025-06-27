using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Leads.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "arquivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataUpload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantidadeLeads = table.Column<int>(type: "integer", nullable: true),
                    ErroProcessamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arquivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "checkouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailCliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pago = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataAbertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SituacaoCadastral = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CNPJ = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CNPJRaiz = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AtividadePrincipalCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AtividadePrincipalDescricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContatoTelefone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContatoTelefoneTipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContatoEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoNaturezaJuridica = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescricaoNaturezaJuridica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CEP = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CapitalSocial = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuadroSocietario1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuadroSocietario2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MatrizFilial = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MEI = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Porte = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Duplicado = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "planos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LimiteExportacoesMes = table.Column<int>(type: "integer", nullable: false),
                    LimiteLeadsPorExportacao = table.Column<int>(type: "integer", nullable: false),
                    PermiteExportarEmail = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteExportarTelefone = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteFiltrosAvancados = table.Column<bool>(type: "boolean", nullable: false),
                    SuportePrioritario = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CNPJ = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Endereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CEP = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    PlanoId = table.Column<int>(type: "integer", nullable: false),
                    ExportacoesRealizadasMes = table.Column<int>(type: "integer", nullable: false),
                    UltimaResetMensal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusPagamento = table.Column<string>(type: "text", nullable: false),
                    DataUltimoPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clientes_planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Perfil = table.Column<string>(type: "text", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuarios_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "historico_exportacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DataExportacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantidadeLeads = table.Column<int>(type: "integer", nullable: false),
                    FiltrosUtilizados = table.Column<string>(type: "text", nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmailDestino = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnviadoPorEmail = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MensagemErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PlanoNome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LimiteDisponivel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_exportacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historico_exportacoes_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historico_exportacoes_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_cnpj",
                table: "clientes",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clientes_PlanoId",
                table: "clientes",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_historico_exportacoes_ClienteId",
                table: "historico_exportacoes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_historico_exportacoes_UsuarioId",
                table: "historico_exportacoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "ix_leads_cnpj",
                table: "leads",
                column: "CNPJ");

            migrationBuilder.CreateIndex(
                name: "ix_leads_email",
                table: "leads",
                column: "ContatoEmail");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_ClienteId",
                table: "usuarios",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arquivos");

            migrationBuilder.DropTable(
                name: "checkouts");

            migrationBuilder.DropTable(
                name: "historico_exportacoes");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "planos");
        }
    }
}
