namespace Application.Common.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string msg) : base(msg)
        {
        }
    }
}
