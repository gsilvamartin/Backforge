namespace Backforge.Core.Exceptions;

public class RequirementAnalysisException : Exception
{
    public RequirementAnalysisException(string message) : base(message) { }
    public RequirementAnalysisException(string message, Exception innerException) : base(message, innerException) { }
}