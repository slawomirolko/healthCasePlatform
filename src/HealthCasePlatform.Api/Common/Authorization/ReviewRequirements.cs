using Microsoft.AspNetCore.Authorization;

namespace HealthCasePlatform.Api.Common.Authorization;

public sealed class ReviewScientificRequirement : IAuthorizationRequirement;
public sealed class ReviewLegalRequirement : IAuthorizationRequirement;
