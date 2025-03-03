namespace CorretorCartaoResposta.Model
{
    public record ResultadoCorrecaoCartaoResposta(string Inscricao, Dictionary<int, ResultadoCorrecaoQuestao> Resultado);
}
