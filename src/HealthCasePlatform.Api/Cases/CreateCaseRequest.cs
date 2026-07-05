using System.ComponentModel.DataAnnotations;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Api.Cases;

public sealed record CreateCaseRequest(
    [property: Required] string Title,
    string? Description,
    [property: Required] Guid CaseTypeId,
    CasePriority Priority,
    [property: Required] string CreatedBy);
