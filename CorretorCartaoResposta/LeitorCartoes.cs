using CorretorCartaoResposta.Model;
using OpenCvSharp;

namespace CorretorCartaoResposta
{
    public class LeitorCartoes
    {
        public static CartaoResposta LerCartaoResposta(string caminhoArquivo)
        {
            using var stream = new StreamReader(caminhoArquivo);
            return LerCartaoResposta(stream.BaseStream);
        }

        public static CartaoResposta LerCartaoResposta(Stream cartaoResposta)
        {
            return new CartaoResposta(imagem: Mat.FromStream(cartaoResposta, ImreadModes.Color));
        }
    }
}
