using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_eventos.Models
{
    public class Evento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome pode ter no máximo 100 caracteres")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "O local é obrigatório.")]
        [StringLength(100, ErrorMessage = "O local pode ter no máximo 100 caracteres")]
        public string Local { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(300, ErrorMessage = "A descrição pode ter no máximo 300 caracteres")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "O tipo do evento é obrigatório")]
        [StringLength(100, ErrorMessage = "O tipo do evento é obrigatório")]
        public string TipoEvento { get; set; }

        [Required(ErrorMessage = "O valor do evento é obrigatório")]
        [Range(0.01, 999999.99, ErrorMessage = "Informe um valor válido")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A imagem é obrigatória.")]
        [StringLength(150, ErrorMessage = "O nome pode ter no máximo 100 caracteres")]
        public string ImagemURL { get; set; }
    }
}
