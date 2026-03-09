using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Projeto_eventos.Models
{
    public class Conexao : IdentityDbContext<IdentityUser>
    {
        public Conexao(DbContextOptions<Conexao> options) : base(options)
        {

        }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Ingresso> Ingressos { get; set; }
    }
}
