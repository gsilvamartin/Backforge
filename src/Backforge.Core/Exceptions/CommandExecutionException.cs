namespace Backforge.Core.Exceptions;


/// <summary>
/// Exception thrown when a command fails during project initialization
/// </summary>
public class CommandExecutionException : Exception
{
    /// <summary>
    /// The specific error output from the command
    /// </summary>
    public string CommandError { get; }

    /// <summary>
    /// Creates a new CommandExecutionException
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="commandError">Command error details</param>
    public CommandExecutionException(string message, string commandError)
        : base(message)
    {
        CommandError = commandError;
    }
}