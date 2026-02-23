using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Domain.Entities;

/// <summary>Represents an assistance request to be optimally matched to a provider.</summary>
public class AssistanceRequest : BaseEntity
{
    public string RequestorName { get; private set; } = default!;
    public GeoCoordinate RequestLocation { get; private set; } = default!;
    public ProviderType RequiredType { get; private set; }
    public RequestStatus Status { get; private set; }
    public Guid? AssignedProviderId { get; private set; }
    public Provider? AssignedProvider { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? Notes { get; private set; }

    private AssistanceRequest() { }

    public static AssistanceRequest Create(
        string requestorName,
        GeoCoordinate location,
        ProviderType requiredType,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(requestorName))
            throw new ArgumentException("Requestor name is required.", nameof(requestorName));

        return new AssistanceRequest
        {
            Id = Guid.NewGuid(),
            RequestorName = requestorName,
            RequestLocation = location ?? throw new ArgumentNullException(nameof(location)),
            RequiredType = requiredType,
            Status = RequestStatus.Pending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AssignProvider(Guid providerId)
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException("Request is not in pending status.");

        AssignedProviderId = providerId;
        Status = RequestStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != RequestStatus.Assigned)
            throw new InvalidOperationException("Request must be assigned before completing.");
        Status = RequestStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == RequestStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed request.");
        Status = RequestStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum RequestStatus
{
    Pending = 1,
    Assigned = 2,
    Completed = 3,
    Cancelled = 4
}
