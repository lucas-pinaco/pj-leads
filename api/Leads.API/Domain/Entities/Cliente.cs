using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leads.API.Domain.Entities
{
    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string RazaoSocial { get; set; }

        [StringLength(200)]
        public string NomeFantasia { get; set; }

        [Required]
        [StringLength(14)]
        public string CNPJ { get; set; }

        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Telefone { get; set; }

        [StringLength(200)]
        public string Endereco { get; set; }

        [StringLength(100)]
        public string Cidade { get; set; }

        [StringLength(2)]
        public string Estado { get; set; }

        [StringLength(9)]
        public string CEP { get; set; }

        [Required]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        // Relacionamento com Plano
        public int PlanoId { get; set; }
        public virtual Plano Plano { get; set; }

        // Relacionamento com Usuários
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

        // Relacionamento com Exportações
        public virtual ICollection<HistoricoExportacao> Exportacoes { get; set; } = new List<HistoricoExportacao>();

        // Controle de Limites
        public int ExportacoesRealizadasMes { get; set; } = 0;
        public DateTime? UltimaResetMensal { get; set; }

        // Informações de Pagamento
        public DateTime? DataVencimento { get; set; }
        public string StatusPagamento { get; set; } = "Pendente"; // Pendente, Pago, Vencido
        public DateTime? DataUltimoPagamento { get; set; }
    }
}