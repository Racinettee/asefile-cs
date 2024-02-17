namespace Asefile;

public class AseFileReadException : Exception
{
    private string _message;
    public override string Message => $"AseFile read exception: {_message}";

    public AseFileReadException(string msg)
    {
        _message = msg;
    }
}