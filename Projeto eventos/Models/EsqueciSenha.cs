using System.ComponentModel.DataAnnotations;

namespace Projeto_eventos.Models
{
    public class EsqueciSenha
    {
        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string? Email { get; set; }
    }
}
