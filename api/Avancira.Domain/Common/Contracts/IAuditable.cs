namespace Avancira.Domain.Common.Contracts;
public interface IAuditable
{
    DateTimeOffset Created { get; }
    Guid CreatedBy { get; }
    DateTimeOffset LastModified { get; }
    Guid? LastModifiedBy { get; }
}

