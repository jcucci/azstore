namespace AzStore.Core.Exceptions;

/// <summary>
/// Exception thrown when directory service operations fail.
/// </summary>
public class DirectoryServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DirectoryServiceException class.
    /// </summary>
    public DirectoryServiceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DirectoryServiceException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DirectoryServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DirectoryServiceException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DirectoryServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}