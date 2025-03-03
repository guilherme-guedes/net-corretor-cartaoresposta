
# Reconhecimento de respostas - Corretor de cartão resposta

Projeto de estudos com a biblioteca OpenCV Sharp e Tesseract Engine para reconhecimento de matrícula e marcações em cartão-resposta.

## Bibliotecas

#### OpenCV Sharp
O reconhecimento das alternativas é feito utilizando a biblioteca OpenCV Sharp;
Há também o pacote de runtime Windows já que esse teste executou em ambiente Windows.

```
Em ambientes linux o pacote linux deve ser adicionado.
```

#### Tesseract
A detecção do número da matrícula foi feita utilizando a engine Tesseract através da biblioteca tesseract para .NET;

Os dados treinados podem ser obtidos em: https://github.com/tesseract-ocr/tessdata

## Configurações
#### Variáveis de ambiente
- PREENCHIMENTO_MINIMO_MARCACAO : Percentual para considerar a marcação de uma alternativa na forma referente a alternativa da questão no cartão; [Default 57]
- TESSERACT_FOLDER : O endereço da pasta aonde os dados de treinamento do Tesseract estão.

#### Configurações de execução
Na classe ConfiguracoesExecucao é possível setar flags para que sejam exibidas telas para facilitar debug e identificação de problemas e ajuste da parametrização do cartão-resposta.

#### Exemplos
Leitura atravé de arquivo local
```
 int[] gabarito = GabaritoQuestoes(qtdAlternativas: 5, qtdQuestoes: 100);

 var modeloCartao = new ModeloCartaoResposta(QuantidadeColunas: 5, QuantidadeQuestoes: 100, QuantidadeAlternativasPorQuestao: 5);

 var corretor = new Corretor(gabarito, modeloCartao);
 var correcaoCartaoResposta = corretor.Corrigir(LeitorCartoes.LerCartaoResposta("cartao-teste.jpeg"));
```

Leitura através de stream
```
 int[] gabarito = GabaritoQuestoes(qtdAlternativas: 5, qtdQuestoes: 100);

 var modeloCartao = new ModeloCartaoResposta(QuantidadeColunas: 5, QuantidadeQuestoes: 100, QuantidadeAlternativasPorQuestao: 5);

 var corretor = new Corretor(gabarito, modeloCartao);
 var correcaoCartaoResposta = corretor.Corrigir(LeitorCartoes.LerCartaoResposta(streamImagemCartaoResposta));
```