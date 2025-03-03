using OpenCvSharp;

namespace CorretorCartaoResposta.Model
{
    public class CartaoResposta : IDisposable
    {
        public Mat Imagem { get; set; }

        internal CartaoResposta(Mat imagem)
        {
            if (imagem is null)
                throw new ArgumentNullException("Matriz de imagem do cartão resposta não informada.");
            
            Imagem = imagem;
        }

        public void Dispose()
        {
            if(Imagem is not null && !Imagem.IsDisposed)
                Imagem.Dispose();
        }

        ~CartaoResposta()
        {
            this.Dispose();
        }
    }
}
