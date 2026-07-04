using ErrorOr;
using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases;

public sealed class CaseType : Entity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    private CaseType() { }

    public static ErrorOr<CaseType> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CaseTypeErrors.NameEmpty;
        }

        return new CaseType
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description
        };
    }
}
