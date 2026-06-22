namespace VoltsCRM.Application.Common.Exceptions;

/// <summary>Thrown when a requested aggregate/entity does not exist. Mapped to HTTP 404 by the API.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"\"{name}\" ({key}) was not found.") { }
}
