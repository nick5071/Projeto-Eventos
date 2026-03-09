using System.ComponentModel.DataAnnotations;

namespace Projeto_eventos.Models
{
    public class Registro
    {
        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "O campo Confirmação de Senha é obrigatório.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "A senha e a confirmação não coincidem.")]
        public string ConfirmPassword { get; set; }

    }
}
