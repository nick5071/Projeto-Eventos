using System.ComponentModel.DataAnnotations;

namespace Projeto_eventos.Models
{
    public class RedefinirSenha
    {
        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Token inválido")]
        public string? Token { get; set; }
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public string? NovaSenha { get; set; }
        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem.")]
        public string? ConfirmarSenha { get; set; }
    }
}
