namespace SDParamsDescripter.Core.Contracts;
public interface IShell : IDisposable
{
    StreamWriter StandardInput
    {
        get;
    }
    StreamReader StandardOutput
    {
        get;
    }
}
