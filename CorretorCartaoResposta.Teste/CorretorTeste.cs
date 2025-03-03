using CorretorCartaoResposta.Model;

namespace CorretorCartaoResposta.Teste
{
    public class CorretorTeste
    {
        #region Testes

        [Fact]
        public void Deve_Corrigir_Cartao_Com_ImagemFoto_Cortada_Iluminada()
        {
            Environment.SetEnvironmentVariable("PREENCHIMENTO_MINIMO_MARCACAO", "51");
            Environment.SetEnvironmentVariable("TESSERACT_FOLDER", @"..\tessdata-main");

            int[] gabarito = GabaritoQuestoes(qtdAlternativas: 5, qtdQuestoes: 100);

            var modeloCartao = new ModeloCartaoResposta(QuantidadeColunas: 5, QuantidadeQuestoes: 100, QuantidadeAlternativasPorQuestao: 5);

            var corretor = new Corretor(gabarito, modeloCartao);
            var cartaoResposta = corretor.Corrigir(LeitorCartoes.LerCartaoResposta(@"..\testes\cartao-teste77.jpeg"), true);

            Console.ReadKey();
        }

        [Fact]
        public void Deve_Corrigir_Cartao_ImagemScaneada()
        {
            Environment.SetEnvironmentVariable("PREENCHIMENTO_MINIMO_MARCACAO", "51");
            Environment.SetEnvironmentVariable("TESSERACT_FOLDER", @"..\tessdata-main");

            int[] gabarito = GabaritoQuestoes(qtdAlternativas: 5, qtdQuestoes: 100);

            var modeloCartao = new ModeloCartaoResposta(QuantidadeColunas: 5, QuantidadeQuestoes: 100, QuantidadeAlternativasPorQuestao: 5);

            var corretor = new Corretor(gabarito, modeloCartao);
            var cartaoResposta = corretor.Corrigir(LeitorCartoes.LerCartaoResposta(@"..\testes\cartao-scanner.jpeg"), true);

            Console.ReadKey();
        }

        [Fact]
        public void Deve_Corrigir_Cartao_ImagemFoto_Normal()
        {
            Environment.SetEnvironmentVariable("PREENCHIMENTO_MINIMO_MARCACAO", "51");
            Environment.SetEnvironmentVariable("TESSERACT_FOLDER", @"..\tessdata-main");

            int[] gabarito = GabaritoQuestoes(qtdAlternativas: 5, qtdQuestoes: 100);

            var modeloCartao = new ModeloCartaoResposta(QuantidadeColunas: 5, QuantidadeQuestoes: 100, QuantidadeAlternativasPorQuestao: 5);

            var corretor = new Corretor(gabarito, modeloCartao);
            var cartaoResposta = corretor.Corrigir(LeitorCartoes.LerCartaoResposta(@"..\testes\cartao-foto-normal1.jpeg"), true);

            Console.ReadKey();
        }

        #endregion Testes

        #region Auxiliar

        private static int[] GabaritoQuestoes(int qtdQuestoes, int qtdAlternativas)
        {
            int[] gabarito = new int[qtdQuestoes];

            for (int i = 0; i < qtdQuestoes; i++)
                gabarito[i] = Random.Shared.Next(qtdAlternativas - 1);

            return gabarito;
        }

        #endregion Auxiliar
    }
}