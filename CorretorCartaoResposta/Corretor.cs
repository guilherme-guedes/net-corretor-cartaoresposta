using CorretorCartaoResposta.Exceptions;
using CorretorCartaoResposta.Model;
using OpenCvSharp;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

namespace CorretorCartaoResposta
{
    public class Corretor
    {
        #region Atributos

        private readonly int _percentualMinimoPreenchimentoAlternativa;
        private readonly int[] _gabarito;
        private readonly ModeloCartaoResposta _modeloCartao;
        private readonly string _tesseract;
        private readonly Size _tamanhoAreaRespostas = new Size(965, 560);

        #endregion Atributos

        #region Construtor

        public Corretor(int[] gabarito, ModeloCartaoResposta modeloCartao)
        {
            _percentualMinimoPreenchimentoAlternativa = Convert.ToInt32(Environment.GetEnvironmentVariable("PREENCHIMENTO_MINIMO_MARCACAO") ?? "57");

            if (gabarito is null || gabarito.Length == 0)
                throw new ArgumentException("O gabarito não foi informado para o corretor.");
            _gabarito = gabarito;

            if (modeloCartao is null)
                throw new ArgumentException("O modelo do cartão de resposta não foi informado para o corretor.");
            _modeloCartao = modeloCartao;

            _tesseract = Environment.GetEnvironmentVariable("TESSERACT_FOLDER");
            if (_tesseract is null)
                throw new ArgumentException("A pasta do tesseract não foi informada para o corretor.");
        }

        #endregion Construtor

        #region Métodos Públicos

        public ResultadoCorrecaoCartaoResposta Corrigir(CartaoResposta cartaoPreenchido, bool debug = false)
        {
            Mat imagemOriginalImutavel = cartaoPreenchido.Imagem;
            Mat imagemBase = new Mat();
            Mat imagemCinza = new Mat();
            Mat imagemInvertida = new Mat();
            Mat imagemPretoBranco = new Mat();
            Mat imagemAreaRespostas = null;
            List<Mat> telasDebug = null;
            string inscricao = null;
            try
            {
                Cv2.Resize(imagemOriginalImutavel, imagemBase, new Size(1300, 900));

                Cv2.CvtColor(src: imagemBase, dst: imagemCinza, ColorConversionCodes.BGR2GRAY);

                inscricao = ExtrairNumeroInscricao(debug, imagemBase, imagemCinza);

                imagemAreaRespostas = ExtrairImagemRegiaoRespostas(imagemBase, imagemCinza, debug);

                var circulosAlternativas = ObterCirculosAlternativas(imagemAreaRespostas, distanciaMinima: 23, debug: debug);
                ContornarCirculosAlternativas(imagemAreaRespostas, circulosAlternativas, Scalar.Black);
                var questoesComAlternativas = MatrizarEOrdenarAlternativas(circulosAlternativas, debug);

                if (ImagemClara(imagemOriginalImutavel))
                {
                    Cv2.AdaptiveThreshold(imagemAreaRespostas, imagemPretoBranco, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 27, 11);
                    Cv2.BitwiseNot(imagemPretoBranco, imagemInvertida);
                }
                else
                {
                    Cv2.AdaptiveThreshold(imagemAreaRespostas, imagemPretoBranco, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 27, 5);
                    Cv2.BitwiseNot(imagemPretoBranco, imagemInvertida);
                }

                if (debug)
                {
                    telasDebug = new List<Mat>();
                    if (ConfiguracoesExecucao.DebugarTelaOriginal)
                        MostrarTela("Original", imagemBase);
                    if (ConfiguracoesExecucao.DebugarAreaRespostas)
                        MostrarTela("Região de respostas", imagemAreaRespostas);
                    if (ConfiguracoesExecucao.DebugarTelaBinaria)
                        MostrarTela("Binaria", imagemPretoBranco);
                    if (ConfiguracoesExecucao.DebugarTelaInvertida)
                        MostrarTela("Invertida", imagemInvertida);
                }

                var resultadoCartaoResposta = VerificarMarcacoesAlternativas(imagemInvertida, circulosQuestoes: questoesComAlternativas);

                if (debug)
                    ExibirImagensFinais(cartaoPreenchido.Imagem, imagemInvertida, questoesComAlternativas, resultadoCartaoResposta, inscricao);

                return new ResultadoCorrecaoCartaoResposta(inscricao, resultadoCartaoResposta);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new ErroCorrecaoException($"Erro ao tentar realizar a correção do cartão resposta. Detalhes: {e.Message}", e);
            }
            finally
            {
                if (debug)
                    Cv2.WaitKey();

                if (debug)
                    if (telasDebug?.Count > 0)
                        foreach (var telaDebug in telasDebug)
                            telaDebug.Dispose();

                Cv2.DestroyAllWindows();
                imagemAreaRespostas?.Dispose();
                imagemInvertida?.Dispose();
                imagemCinza?.Dispose();
                imagemPretoBranco?.Dispose();
                cartaoPreenchido.Dispose();
                imagemBase?.Dispose();
            }

            throw new ErroCorrecaoException("Erro ao tentar realizar a correção do cartão resposta");
        }

        #endregion Métodos Públicos

        #region Métodos Privados

        private string ExtrairNumeroInscricao(bool debug, Mat imagemOriginal, Mat imagemCinza)
        {
            Mat imagemTreshold = new Mat();
            Mat invertida = new Mat();
            Mat areaInscricao = new Mat();

            try
            {
                areaInscricao = ExtrairImagemRegiaoInscricao(imagemOriginal, imagemCinza, debug);

                Cv2.AdaptiveThreshold(areaInscricao, imagemTreshold, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 23, 11);
                Cv2.BitwiseNot(imagemTreshold, invertida);

                using (var engine = new Tesseract.TesseractEngine(_tesseract, "por", global::Tesseract.EngineMode.TesseractAndLstm))
                {
                    byte[] buffer = invertida.ToBytes();
                    using (var img = Tesseract.Pix.LoadFromMemory(buffer))
                    {
                        using (var pageTest = engine.Process(img, Tesseract.PageSegMode.Auto))
                        {
                            var text = pageTest.GetText();
                            var numeroInscricao = new Regex("[0-9]+").Match(text);

                            if (debug)
                                Console.WriteLine($"Matrícula: {numeroInscricao.Value}");

                            return numeroInscricao.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ErroCorrecaoException($"Erro ao extrair inscrição do cartão resposta. Detalhes: {e.Message}", e);
            }
            finally
            {
                imagemTreshold?.Dispose();
                invertida?.Dispose();
                areaInscricao?.Dispose();
            }
        }

        private Mat ExtrairImagemRegiaoInscricao(Mat imagemOriginal, Mat imagemCinza, bool debug)
        {
            Mat imagemBorda = new Mat();
            Mat imagemAreaNome = null;
            Mat imagemContornos = new Mat();

            try
            {
                var contornos = ExtrairContornos(imagemOriginal, imagemCinza, imagemBorda).Skip(2).ToArray();
                imagemOriginal.CopyTo(imagemContornos);

                foreach (var contorno in contornos)
                {
                    if (imagemAreaNome is not null) break;

                    var retanguloContorno = Cv2.BoundingRect(contorno);

                    if (ConfiguracoesExecucao.DebugarContornos && debug)
                        Cv2.Rectangle(imagemContornos, retanguloContorno, Scalar.Blue, 2);

                    if (imagemAreaNome is null &&
                        retanguloContorno.Width >= _modeloCartao.LarguraMinimaAreaNome &&
                        retanguloContorno.Height >= _modeloCartao.AlturaMinimaAreaNome)
                    {
                        if (retanguloContorno.Width <= _modeloCartao.LarguraMaximaAreaNome &&
                            retanguloContorno.Height <= _modeloCartao.AlturaMaximaAreaNome)
                            imagemAreaNome = new Mat(imagemCinza, retanguloContorno);
                    }
                }

                if (ConfiguracoesExecucao.DebugarAreaInscricao && debug)
                {
                    if (imagemBorda is not null)
                        MostrarTela("Borda area de inscricao", imagemBorda);
                    if (imagemAreaNome is not null)
                        MostrarTela("Area Inscricao", imagemAreaNome);
                    if (imagemContornos is not null)
                        MostrarTela("Contornos", imagemContornos);

                    Cv2.WaitKey();

                    Cv2.DestroyWindow("Borda area de inscricao");
                    Cv2.DestroyWindow("Area Inscricao");
                    Cv2.DestroyWindow("Contornos");
                }
            }
            catch (Exception e)
            {
                throw new ErroCorrecaoException($"Erro ao extrair área de respostas do cartão resposta. Detalhes: {e.Message}", e);
            }
            finally
            {
                imagemContornos?.Dispose();
                imagemBorda?.Dispose();
            }

            return imagemAreaNome;
        }

        private Mat ExtrairImagemRegiaoRespostas(Mat imagemOriginal, Mat imagemCinza, bool debug)
        {
            Mat imagemBordas = new Mat();
            Mat imagemAreaRespostas = null;
            Mat imagemContornos = new Mat();
            try
            {
                var contornos = ExtrairContornos(imagemOriginal, imagemCinza, imagemBordas);
                imagemOriginal.CopyTo(imagemContornos);

                foreach (var contorno in contornos)
                {
                    if (imagemAreaRespostas is not null) break;

                    var retanguloContorno = Cv2.BoundingRect(contorno);

                    if (ConfiguracoesExecucao.DebugarContornos && debug)
                        Cv2.Rectangle(imagemContornos, retanguloContorno, Scalar.Green, 2);

                    if (imagemAreaRespostas is null &&
                        retanguloContorno.Width >= _modeloCartao.LarguraMinimaAreaRespostas &&
                        retanguloContorno.Height >= _modeloCartao.AlturaMinimaAreaRespostas)
                    {
                        if (retanguloContorno.Height <= _modeloCartao.AlturaMaximaAreaRespostas &&
                            retanguloContorno.Width <= _modeloCartao.LarguraMaximaAreaRespostas)
                        {
                            imagemAreaRespostas = new Mat(imagemCinza, retanguloContorno);
                            Cv2.Resize(imagemAreaRespostas, imagemAreaRespostas, _tamanhoAreaRespostas);
                        }
                    }
                }

                if (ConfiguracoesExecucao.DebugarAreaRespostas && debug)
                {
                    if (imagemBordas is not null)
                        MostrarTela("Area borda respostas", imagemBordas);
                    if (imagemAreaRespostas is not null)
                        MostrarTela("Area respostas", imagemAreaRespostas);
                    if (imagemContornos is not null)
                        MostrarTela("Contornos", imagemContornos);

                    Cv2.WaitKey();

                    Cv2.DestroyWindow("Area borda respostas");
                    Cv2.DestroyWindow("Area respostas");
                    Cv2.DestroyWindow("Contornos");
                }
            }
            finally
            {
                imagemContornos?.Dispose();
                imagemBordas?.Dispose();
            }

            return imagemAreaRespostas;
        }

        private CircleSegment[] ObterCirculosAlternativas(Mat imagemBase, double param1 = 10, double param2 = 20, int distanciaMinima = 16, bool debug = false)
        {
            Mat imagemBinaria = new Mat();
            Mat imagemDilatada = new Mat();
            Mat imagemTratada = new Mat();
            Mat mascara = null;
            try
            {
                Cv2.AdaptiveThreshold(imagemBase, imagemBinaria, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 27, 5);

                mascara = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Cv2.MorphologyEx(imagemBinaria, imagemDilatada, MorphTypes.Dilate, mascara);
                Cv2.Absdiff(imagemBinaria, imagemDilatada, imagemTratada);

                param1 = _modeloCartao.RaioMinimoCirculoAlternativa;
                param2 = _modeloCartao.RaioMaximoCirculoAlternativa;

                var circulosAlternativas = Cv2.HoughCircles(image: imagemTratada,
                                                            method: HoughModes.Gradient,
                                                            dp: 1.0,
                                                            minDist: distanciaMinima,
                                                            param1: param1,
                                                            param2: param2,
                                                            minRadius: _modeloCartao.RaioMinimoCirculoAlternativa,
                                                            maxRadius: _modeloCartao.RaioMaximoCirculoAlternativa);

                if (debug && ConfiguracoesExecucao.DebugarObtencaoCirculos)
                {
                    MostrarTela("Thresh adaptativo", imagemBinaria);
                    MostrarTela("Resultado", imagemTratada);
                    Cv2.WaitKey();
                }

                return circulosAlternativas.OrderBy(x => x.Center.Y).ToArray();
            }
            finally
            {
                mascara?.Dispose();
                imagemDilatada?.Dispose();
                imagemBinaria?.Dispose();
                imagemTratada?.Dispose();
            }
        }

        private CircleSegment[][] MatrizarEOrdenarAlternativas(CircleSegment[] circulosAlternativas, bool debug = false)
        {
            var qtdTotalEncontrada = circulosAlternativas.Count();
            int qtdTotal = _modeloCartao.QuantidadeQuestoes * _modeloCartao.QuantidadeAlternativasPorQuestao;
            int qtdPrimeiraLinha = _modeloCartao.QuantidadeColunas * _modeloCartao.QuantidadeAlternativasPorQuestao;
            var qtdLinhas = (int)Math.Round((decimal)_modeloCartao.QuantidadeQuestoes / _modeloCartao.QuantidadeColunas, 0, MidpointRounding.ToPositiveInfinity); // jogar pra cima

            if (!debug && qtdTotalEncontrada != qtdTotal)
                throw new Exception($"A quantidade de círculos/alternativas encontradas ({qtdTotalEncontrada}) não é igual a quantidade total informada ({_modeloCartao.QuantidadeQuestoes} questões * {_modeloCartao.QuantidadeAlternativasPorQuestao} alternativas).");

            #region Segmentação e organização de circulos/alternativas por linha (Y)

            List<CircleSegment[]> alternativasPorLinha = new List<CircleSegment[]>();
            CircleSegment[] linhaIteradora = circulosAlternativas.Take(qtdPrimeiraLinha).OrderBy(c => c.Center.X).ToArray();
            alternativasPorLinha.Add(linhaIteradora);
            circulosAlternativas = circulosAlternativas.Skip(qtdPrimeiraLinha).ToArray();

            var iLinha = 1;
            do
            {
                var maxYAtual = circulosAlternativas.Take(qtdPrimeiraLinha).Max(ca => ca.Center.Y);

                linhaIteradora = circulosAlternativas.Where(c => c.Center.Y <= maxYAtual).OrderBy(c => c.Center.X).ToArray();
                alternativasPorLinha.Add(linhaIteradora);

                circulosAlternativas = circulosAlternativas.Skip(linhaIteradora.Length).ToArray();
                iLinha++;
            } while (iLinha < qtdLinhas);

            #endregion Segmentação e organização de circulos/alternativas por linha (Y)

            #region Ordenação de circulos/alternativas para segmentar por ordem das questões

            CircleSegment[][] questoesOrdenadas = new CircleSegment[_modeloCartao.QuantidadeQuestoes][];
            iLinha = 0;
            for (int iQuestao = 0, iColuna = 0; iQuestao < _modeloCartao.QuantidadeQuestoes; iQuestao++, iLinha++)
            {
                if (iLinha == qtdLinhas)
                {
                    iColuna++;
                    iLinha = 0;
                }

                CircleSegment[] alternativasQuestao = alternativasPorLinha[iLinha].Skip(iColuna * _modeloCartao.QuantidadeAlternativasPorQuestao).Take(_modeloCartao.QuantidadeAlternativasPorQuestao).ToArray();
                questoesOrdenadas[iQuestao] = alternativasQuestao;
            }

            #endregion Ordenação de circulos/alternativas para segmentar por ordem das questões

            return questoesOrdenadas;
        }

        private Dictionary<int, ResultadoCorrecaoQuestao> VerificarMarcacoesAlternativas(Mat imagemBase, CircleSegment[][] circulosQuestoes)
        {
            Mat imgAlternativa = null;
            Dictionary<int, ResultadoCorrecaoQuestao> resultado = new Dictionary<int, ResultadoCorrecaoQuestao>();
            try
            {
                for (int iQuestao = 0; iQuestao < _modeloCartao.QuantidadeQuestoes; iQuestao++)
                {
                    resultado[iQuestao] = new ResultadoCorrecaoQuestao(iQuestao, _gabarito[iQuestao]);

                    for (int iAlternativa = 0; iAlternativa < _modeloCartao.QuantidadeAlternativasPorQuestao; iAlternativa++)
                    {
                        imgAlternativa = new Mat(imagemBase, ExtrairRetanguloAlternativa(circulosQuestoes[iQuestao][iAlternativa]));
                        if (AlternativaMarcada(imgAlternativa))
                        {
                            if (resultado[iQuestao].TemAlternativaMarcada())
                            {
                                resultado[iQuestao].AlternativaMarcada = null;
                                break;
                            }

                            resultado[iQuestao].AlternativaMarcada = iAlternativa;
                            continue;
                        }
                    }
                }
            }
            finally
            {
                imgAlternativa?.Dispose();
            }
            return resultado;
        }

        private Rect ExtrairRetanguloAlternativa(CircleSegment alternativa) =>
            new Rect(X: (int)(alternativa.Center.X - alternativa.Radius),
                    Y: (int)(alternativa.Center.Y - alternativa.Radius),
                    Width: (int)alternativa.Radius * 2 + 1, (int)alternativa.Radius * 2 + 1);

        private bool AlternativaMarcada(Mat roiAlternativa)
        {
            int qtdAtivos = Cv2.CountNonZero(roiAlternativa);
            decimal proporcao = decimal.Divide(qtdAtivos, roiAlternativa.Height * roiAlternativa.Width);
            return Math.Round(proporcao * 100, 2) > _percentualMinimoPreenchimentoAlternativa;
        }

        private Point[][] ExtrairContornos(Mat imagemOriginal, Mat imagemCinza, Mat imagemBorda)
        {
            if (ImagemClara(imagemOriginal))
                Cv2.Canny(src: imagemCinza, imagemBorda, 125, 350);
            else
                Cv2.Canny(src: imagemCinza, imagemBorda, 60, 70, 5);

            Point[][] contornos;
            HierarchyIndex[] indices;
            Cv2.FindContours(imagemBorda, out contornos, out indices, RetrievalModes.External, ContourApproximationModes.ApproxNone);

            return contornos.OrderByDescending(c => Cv2.ContourArea(c)).ToArray();
        }

        private bool ImagemClara(Mat imagem)
        {
            var aux = Cv2.Mean(imagem);
            return aux.ToDouble() > 160;
        }

        #region Exibição

        private void ContornarCirculosAlternativas(Mat imagemFonte, CircleSegment[] circulos, Scalar cor, int espessura = 1)
        {
            foreach (var circulo in circulos)
            {
                Cv2.Circle(img: imagemFonte,
                    centerX: (int)circulo.Center.X,
                    centerY: (int)circulo.Center.Y,
                    radius: (int)circulo.Radius,
                    color: cor,
                    thickness: espessura);
            }
        }

        private void MostrarTela(string tituloTela, Mat imagemExibida) =>
                    Cv2.ImShow(tituloTela, imagemExibida);

        #endregion Exibição

        #region Debug

        private void ExibirImagensFinais(Mat imagemOriginal,
            Mat imgInvertida,
            CircleSegment[][] questoesComAlternativas,
            Dictionary<int, ResultadoCorrecaoQuestao> resultado,
            string inscricao)
        {
            Mat imgCartao = null;
            Mat imgGabarito = null;
            if (questoesComAlternativas is not null && ConfiguracoesExecucao.DebugarTelaRespostas)
            {
                imgCartao = CriarImagemVerificacaoCartao(imgInvertida, questoesComAlternativas, resultado, inscricao);
                MostrarTela("Cartão confirmação", imgCartao);

                imgGabarito = CriarImagemGabarito(imgInvertida, questoesComAlternativas, _gabarito);
                MostrarTela("Gabarito", imgGabarito);
            }

            if (ConfiguracoesExecucao.DebugarTelaOriginal)
                MostrarTela("Original", imagemOriginal);

            Cv2.WaitKey();

            imgCartao?.Dispose();
            imgGabarito?.Dispose();
        }

        private Mat CriarImagemGabarito(Mat imgReferencia,
           CircleSegment[][] questoesComAlternativas,
           int[] gabarito)
        {
            if (questoesComAlternativas is not null && questoesComAlternativas.Length > 0)
            {
                Mat imgCartaoGabarito = new Mat(imgReferencia.Size(), imgReferencia.Type(), Scalar.Black);
                for (int iQuestao = 0; iQuestao < questoesComAlternativas.Length; iQuestao++)
                {
                    for (int iAlternativa = 0; iAlternativa < _modeloCartao.QuantidadeAlternativasPorQuestao; iAlternativa++)
                    {
                        var alternativa = questoesComAlternativas[iQuestao][iAlternativa];
                        if (gabarito[iQuestao] == iAlternativa)
                            DesenharAlternativaMarcada(imgCartaoGabarito, alternativa);
                        else
                            DesenharAlternativaNaoMarcada(imgCartaoGabarito, alternativa);
                    }
                }

                return imgCartaoGabarito;
            }
            return null;
        }

        private Mat CriarImagemVerificacaoCartao(Mat imgReferencia,
            CircleSegment[][] questoesComAlternativas,
            Dictionary<int, ResultadoCorrecaoQuestao> resultado,
            string inscricao)
        {
            int margemAltura = 60;
            if (questoesComAlternativas is not null && questoesComAlternativas.Length > 0)
            {
                var tamanho = imgReferencia.Size();
                Mat imgCartao = new Mat(tamanho.Height + margemAltura, tamanho.Width, imgReferencia.Type(), Scalar.Black);
                int acertos = 0;
                for (int iQuestao = 0; iQuestao < questoesComAlternativas.Length; iQuestao++)
                {
                    for (int iAlternativa = 0; iAlternativa < _modeloCartao.QuantidadeAlternativasPorQuestao; iAlternativa++)
                    {
                        var alternativa = questoesComAlternativas[iQuestao][iAlternativa];
                        if (resultado[iQuestao].AlternativaMarcada.HasValue &&
                            resultado[iQuestao].AlternativaMarcada!.Value == iAlternativa)
                            DesenharAlternativaMarcada(imgCartao, alternativa);
                        else
                            DesenharAlternativaNaoMarcada(imgCartao, alternativa);
                    }
                    if (resultado[iQuestao].Acertou())
                        acertos++;
                }

                Cv2.PutText(imgCartao, $"Inscricao: {inscricao}", new Point(X: 5, Y: tamanho.Height + (margemAltura - 40)), HersheyFonts.HersheyPlain, 1, Scalar.White, 1);
                Cv2.PutText(imgCartao, $"Acertos: {acertos}", new Point(X: 5, Y: tamanho.Height + (margemAltura - 20)), HersheyFonts.HersheyPlain, 1, Scalar.White, 1);

                return imgCartao;
            }
            return null;
        }

        private void DesenharAlternativaNaoMarcada(Mat imgCartao, CircleSegment alternativa) =>
            Cv2.Circle(img: imgCartao,
                        center: alternativa.Center.ToPoint(),
                        radius: (int)alternativa.Radius,
                        color: Scalar.White,
                        thickness: 1);

        private void DesenharAlternativaMarcada(Mat imgCartao, CircleSegment alternativa) =>
            Cv2.Circle(img: imgCartao,
                        center: alternativa.Center.ToPoint(),
                        radius: (int)alternativa.Radius,
                        color: Scalar.White,
                        thickness: -1);

        #endregion Debug

        #endregion Métodos Privados
    }
}