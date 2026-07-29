using System.Net;
using System.Net.Http;
using System.Text.Json;
using LionSimPlanner.Personnel.Infrastructure.CmsSync;
using LionSimPlanner.Personnel.Infrastructure.Handlers;
using LionSimPlanner.Shared.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace LionSimPlanner.Personnel.Application.Tests.Handlers;

public sealed class HandleTrainingRecordCompletedHandlerTests
{
    private static TrainingRecordCompletedNotification CreateNotification() => new(
        SessionId: Guid.NewGuid(),
        EmployeeCode: "PLT001",
        SyllabusId: "B737_RecurrentTraining",
        IsGraded: true,
        GradeStatus: "PASSED",
        CompletionDate: new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc),
        InstructorNotes: "Well executed approach and landing.");

    private static (Mock<HttpMessageHandler> Handler, CmsApiClient Client) CreateCmsApiClient()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var logger = Mock.Of<ILogger<CmsApiClient>>();
        var client = new CmsApiClient(httpClient, logger);

        return (mockHandler, client);
    }

    // ─────────────────────────────────────────────
    //  Successful CMS POST
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidNotification_PostsTrainingRecordToCms()
    {
        var (mockHandler, cmsClient) = CreateCmsApiClient();
        var notification = CreateNotification();

        var sut = new HandleTrainingRecordCompletedHandler(
            cmsClient,
            Mock.Of<ILogger<HandleTrainingRecordCompletedHandler>>());

        await sut.Handle(notification, CancellationToken.None);

        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.PathAndQuery == "/api/v1/cms/training/records"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidNotification_SendsCorrectPayload()
    {
        var (mockHandler, cmsClient) = CreateCmsApiClient();
        var notification = CreateNotification();

        HttpRequestMessage? captured = null;
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new HandleTrainingRecordCompletedHandler(
            cmsClient,
            Mock.Of<ILogger<HandleTrainingRecordCompletedHandler>>());

        await sut.Handle(notification, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().NotBeNull();

        var body = await captured.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<CmsTrainingRecordPayload>(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        payload.Should().NotBeNull();
        payload!.SessionId.Should().Be(notification.SessionId.ToString());
        payload.EmployeeCode.Should().Be("PLT001");
        payload.SyllabusId.Should().Be("B737_RecurrentTraining");
        payload.IsGraded.Should().BeTrue();
        payload.GradeStatus.Should().Be("PASSED");
        payload.InstructorNotes.Should().Be("Well executed approach and landing.");
    }

    // ─────────────────────────────────────────────
    //  CMS failure tolerance
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_CmsReturnsServerError_DoesNotThrow()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost") };
        var cmsClient = new CmsApiClient(httpClient, Mock.Of<ILogger<CmsApiClient>>());
        var notification = CreateNotification();

        var sut = new HandleTrainingRecordCompletedHandler(
            cmsClient,
            Mock.Of<ILogger<HandleTrainingRecordCompletedHandler>>());

        var act = async () => await sut.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_CmsThrowsException_DoesNotThrow()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost") };
        var cmsClient = new CmsApiClient(httpClient, Mock.Of<ILogger<CmsApiClient>>());
        var notification = CreateNotification();

        var sut = new HandleTrainingRecordCompletedHandler(
            cmsClient,
            Mock.Of<ILogger<HandleTrainingRecordCompletedHandler>>());

        var act = async () => await sut.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
