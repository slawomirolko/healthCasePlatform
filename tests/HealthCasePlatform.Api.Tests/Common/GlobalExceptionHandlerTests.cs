using System.Text.Json;
using HealthCasePlatform.Api.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Common;

public sealed class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, IServiceProvider Services) CreateHandler(bool isDevelopment)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();
        var problemDetailsService = provider.GetRequiredService<IProblemDetailsService>();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);
        return (new GlobalExceptionHandler(problemDetailsService, environment), provider);
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(Stream body)
    {
        body.Position = 0;
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(body);
    }

    [Fact]
    public async Task TryHandleAsync_InDevelopment_Returns500WithExceptionDetail()
    {
        var (handler, services) = CreateHandler(isDevelopment: true);
        var httpContext = CreateHttpContext(services);
        var exception = new InvalidOperationException("kaboom");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        httpContext.Response.ContentType.ShouldStartWith("application/problem+json");

        var problem = await ReadProblemDetailsAsync(httpContext.Response.Body);
        problem.ShouldNotBeNull();
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("kaboom");
    }

    [Fact]
    public async Task TryHandleAsync_InProduction_Returns500WithoutExceptionDetail()
    {
        var (handler, services) = CreateHandler(isDevelopment: false);
        var httpContext = CreateHttpContext(services);
        var exception = new InvalidOperationException("kaboom");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        var problem = await ReadProblemDetailsAsync(httpContext.Response.Body);
        problem.ShouldNotBeNull();
        problem.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task TryHandleAsync_OnCancellation_ReturnsTrueWithoutWriting500()
    {
        var (handler, services) = CreateHandler(isDevelopment: true);
        var httpContext = CreateHttpContext(services);
        var exception = new OperationCanceledException();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        httpContext.Response.Body.Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryHandleAsync_OnMalformedRequestBody_Returns400WithoutExceptionDetail(bool isDevelopment)
    {
        var (handler, services) = CreateHandler(isDevelopment);
        var httpContext = CreateHttpContext(services);
        var exception = new BadHttpRequestException("invalid JSON");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.ShouldStartWith("application/problem+json");

        var problem = await ReadProblemDetailsAsync(httpContext.Response.Body);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Detail.ShouldBeNull();
    }
}
