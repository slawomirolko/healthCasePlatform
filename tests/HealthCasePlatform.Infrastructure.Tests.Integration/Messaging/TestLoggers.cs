using Microsoft.Extensions.Logging;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Messaging;

internal static class TestLoggers
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(_ => { });

    public static ILogger<T> Create<T>() => Factory.CreateLogger<T>();
}
