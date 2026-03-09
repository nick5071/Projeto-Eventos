namespace Projeto_eventos.Models
{
    public class SucessoPagamentoViewModel
    {
        public Evento Evento { get; set; }
        public PagamentoViewModel EventoDados { get; set; }
        public decimal ValorFinal { get; set; }
    }
}
