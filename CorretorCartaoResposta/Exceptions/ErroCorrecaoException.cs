namespace CorretorCartaoResposta.Exceptions
{
    public class ErroCorrecaoException : Exception
    {
        public ErroCorrecaoException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
