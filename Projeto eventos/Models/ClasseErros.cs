using Microsoft.AspNetCore.Identity;

namespace Projeto_eventos.Models
{
    public class ClasseErros : IdentityErrorDescriber
    {
        public override IdentityError PasswordRequiresUpper()
       => new IdentityError
       {
           Code = nameof(PasswordRequiresUpper),
           Description = "A senha deve conter pelo menos uma letra maiúscula."
       };

        public override IdentityError PasswordRequiresLower()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "A senha deve conter pelo menos uma letra minúscula."
            };

        public override IdentityError PasswordTooShort(int length)
            => new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"A senha deve ter no mínimo {length} caracteres."
            };

    }
}
