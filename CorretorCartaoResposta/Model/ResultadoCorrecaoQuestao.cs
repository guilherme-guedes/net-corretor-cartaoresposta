namespace CorretorCartaoResposta.Model
{
    public class ResultadoCorrecaoQuestao
    {
        public int IndiceQuestao { get; private set; }
        public int AlternativaCorreta { get; private set; }
        public int? AlternativaMarcada { get; set; }

        public ResultadoCorrecaoQuestao(int indiceQuestao, int alternativaCorreta, int? alternativaMarcada = null)
        {
            this.IndiceQuestao = indiceQuestao;
            this.AlternativaCorreta = alternativaCorreta;
            this.AlternativaMarcada = alternativaMarcada;
        }

        public bool TemAlternativaMarcada() => this.AlternativaMarcada.HasValue;

        public bool Acertou() => this.TemAlternativaMarcada() && this.AlternativaMarcada!.Value == this.AlternativaCorreta;
    }
}
