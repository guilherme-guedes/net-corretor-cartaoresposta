namespace CorretorCartaoResposta.Model
{
    public record ModeloCartaoResposta(int QuantidadeColunas, 
        int QuantidadeQuestoes, 
        int QuantidadeAlternativasPorQuestao, 
        int RaioMinimoCirculoAlternativa = 8,
        int RaioMaximoCirculoAlternativa = 12,
        int AlturaMinimaAreaRespostas = 180,
        int AlturaMaximaAreaRespostas = 1000,
        int LarguraMinimaAreaRespostas = 300,
        int LarguraMaximaAreaRespostas = 1500,
        int AlturaMinimaAreaNome = 80,
        int AlturaMaximaAreaNome = 150,
        int LarguraMinimaAreaNome = 150,
        int LarguraMaximaAreaNome = 220);
}
