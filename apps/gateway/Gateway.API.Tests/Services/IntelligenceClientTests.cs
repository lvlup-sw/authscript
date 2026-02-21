using System.Net;
using System.Text;
using System.Text.Json;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for IntelligenceClient HTTP behavior: verifying that it
/// calls the correct endpoint, serializes/deserializes correctly, and handles errors.
/// </summary>
public class IntelligenceClientTests
{
    private static (IntelligenceClient client, MockHttpHandler handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
    {
        var handler = new MockHttpHandler(responseFactory);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test-intelligence:8000") };
        var logger = NullLogger<IntelligenceClient>.Instance;
        return (new IntelligenceClient(httpClient, logger), handler);
    }

    private static ClinicalBundle CreateTestBundle()
    {
        return new ClinicalBundle
        {
            PatientId = "patient-123",
            Patient = new PatientInfo
            {
                Id = "patient-123",
                GivenName = "Donna",
                FamilyName = "Sandbox",
                BirthDate = new DateOnly(1968, 3, 15),
                Gender = "female",
                MemberId = "ATH60178"
            },
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "cond-1",
                    Code = "M54.5",
                    Display = "Low back pain",
                    ClinicalStatus = "active"
                }
            ],
            Observations =
            [
                new ObservationInfo
                {
                    Id = "obs-1",
                    Code = "72166-2",
                    Display = "Smoking status",
                    Value = "Never smoker"
                }
            ],
            Procedures =
            [
                new ProcedureInfo
                {
                    Id = "proc-1",
                    Code = "99213",
                    Display = "Office visit",
                    Status = "completed"
                }
            ]
        };
    }

    private static readonly string MockResponseJson = """
    {
        "patient_name": "Donna Sandbox",
        "patient_dob": "1968-03-15",
        "member_id": "ATH60178",
        "diagnosis_codes": ["M54.5"],
        "procedure_code": "72148",
        "clinical_summary": "AI-generated clinical summary for MRI lumbar spine.",
        "supporting_evidence": [
            {
                "criterion_id": "conservative_therapy",
                "status": "MET",
                "evidence": "Patient completed 8 weeks of physical therapy",
                "source": "Clinical notes",
                "confidence": 0.95
            }
        ],
        "recommendation": "APPROVE",
        "confidence_score": 0.92,
        "field_mappings": {"PatientName": "Donna Sandbox"}
    }
    """;

    [Test]
    public async Task AnalyzeAsync_PostsToAnalyzeEndpoint_WithCorrectPayload()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var (client, _) = CreateClient(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MockResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var bundle = CreateTestBundle();

        // Act
        await client.AnalyzeAsync(bundle, "72148");

        // Assert - Correct URL and method
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(capturedRequest.RequestUri!.AbsolutePath).IsEqualTo("/api/analyze");

        // Assert - Body contains snake_case keys
        await Assert.That(capturedBody).IsNotNull();
        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("patient_id", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("procedure_code", out _)).IsTrue();
        await Assert.That(root.GetProperty("patient_id").GetString()).IsEqualTo("patient-123");
        await Assert.That(root.GetProperty("procedure_code").GetString()).IsEqualTo("72148");
    }

    [Test]
    public async Task AnalyzeAsync_DeserializesResponse_ToPAFormData()
    {
        // Arrange
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MockResponseJson, Encoding.UTF8, "application/json")
            }));

        var bundle = CreateTestBundle();

        // Act
        var result = await client.AnalyzeAsync(bundle, "72148");

        // Assert
        await Assert.That(result.PatientName).IsEqualTo("Donna Sandbox");
        await Assert.That(result.PatientDob).IsEqualTo("1968-03-15");
        await Assert.That(result.MemberId).IsEqualTo("ATH60178");
        await Assert.That(result.DiagnosisCodes).Contains("M54.5");
        await Assert.That(result.ProcedureCode).IsEqualTo("72148");
        await Assert.That(result.ClinicalSummary).IsEqualTo("AI-generated clinical summary for MRI lumbar spine.");
        await Assert.That(result.Recommendation).IsEqualTo("APPROVE");
        await Assert.That(result.ConfidenceScore).IsEqualTo(0.92);
        await Assert.That(result.SupportingEvidence).HasCount().EqualTo(1);
        await Assert.That(result.SupportingEvidence[0].CriterionId).IsEqualTo("conservative_therapy");
        await Assert.That(result.SupportingEvidence[0].Status).IsEqualTo("MET");
        await Assert.That(result.SupportingEvidence[0].Confidence).IsEqualTo(0.95);
        await Assert.That(result.FieldMappings["PatientName"]).IsEqualTo("Donna Sandbox");
    }

    [Test]
    public async Task AnalyzeAsync_WhenServiceReturns400_ThrowsHttpRequestException()
    {
        // Arrange
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"detail\":\"Invalid request\"}", Encoding.UTF8, "application/json")
            }));

        var bundle = CreateTestBundle();

        // Act & Assert
        await Assert.That(() => client.AnalyzeAsync(bundle, "72148"))
            .Throws<HttpRequestException>();
    }

    [Test]
    public async Task AnalyzeAsync_WhenServiceReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"detail\":\"Internal error\"}", Encoding.UTF8, "application/json")
            }));

        var bundle = CreateTestBundle();

        // Act & Assert
        await Assert.That(() => client.AnalyzeAsync(bundle, "72148"))
            .Throws<HttpRequestException>();
    }
}

internal sealed class MockHttpHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => _handler(request);
}
