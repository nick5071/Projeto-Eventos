using System.ComponentModel.DataAnnotations;

namespace Projeto_eventos.Models
{
    public class PagamentoViewModel
    {
        public int EventoId { get; set; }
        public string CardToken { get; set; }
        public string CPF { get; set; }
        public string Nome { get; set; }
        public string TipoIngresso { get; set; }
        public string Rua { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
    }
}
