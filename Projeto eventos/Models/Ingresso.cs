using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_eventos.Models
{
    public class Ingresso
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int EventoId { get; set; }
        [ForeignKey(nameof(EventoId))]
        public Evento Evento { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public IdentityUser Usuario { get; set; }
        [Required]
        [MaxLength(20)]
        public string TipoIngresso { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorPago { get; set; }

        [Required]
        public DateTime DataCompra { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ativo";
    }
}
