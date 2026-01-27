using Gateway.API.Abstractions;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Endpoints;

public static class CdsHooksEndpoints
{
    // MRI Lumbar CPT codes we handle
    private static readonly HashSet<string> SupportedProcedureCodes = ["72148", "72149", "72158"];

    public static void MapCdsHooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cds-services")
            .WithTags("CDS Hooks");

        // Discovery endpoint - Epic registers this
        group.MapGet("/", GetDiscoveryDocument)
            .WithName("GetCdsServices")
            .WithSummary("CDS Hooks discovery endpoint");

        // Individual service discovery
        group.MapGet("/authscript", GetServiceDefinition)
            .WithName("GetAuthScriptService")
            .WithSummary("AuthScript service definition");

        // Order-select hook endpoint
        group.MapPost("/authscript", HandleOrderSelect)
            .WithName("HandleOrderSelect")
            .WithSummary("Handle order-select CDS Hook from Epic");
    }

    private static IResult GetDiscoveryDocument()
    {
        var discovery = new
        {
            services = new[]
            {
                new
                {
                    id = "authscript",
                    hook = "order-select",
                    title = "AuthScript Prior Authorization",
                    description = "AI-powered prior authorization form completion for MRI Lumbar Spine",
                    prefetch = new
                    {
                        patient = "Patient/{{context.patientId}}",
                        serviceRequest = "ServiceRequest?_id={{context.draftOrders.ServiceRequest.id}}"
                    }
                }
            }
        };

        return Results.Ok(discovery);
    }

    private static IResult GetServiceDefinition()
    {
        var service = new
        {
            id = "authscript",
            hook = "order-select",
            title = "AuthScript Prior Authorization",
            description = "AI-powered prior authorization form completion for MRI Lumbar Spine",
            prefetch = new
            {
                patient = "Patient/{{context.patientId}}",
                serviceRequest = "ServiceRequest?_id={{context.draftOrders.ServiceRequest.id}}"
            }
        };

        return Results.Ok(service);
    }

    private static async Task<IResult> HandleOrderSelect(
        [FromBody] CdsRequest request,
        [FromServices] IFhirDataAggregator fhirAggregator,
        [FromServices] IIntelligenceClient intelligenceClient,
        [FromServices] IPdfFormStamper pdfStamper,
        [FromServices] IEpicUploader epicUploader,
        [FromServices] IDemoCacheService cacheService,
        [FromServices] IConfiguration config,
        [FromServices] ILogger<CdsRequest> logger,
        CancellationToken cancellationToken)
    {
        var transactionId = $"txn-{Guid.NewGuid():N}";

        logger.LogInformation(
            "Received order-select hook. TransactionId={TransactionId}, PatientId={PatientId}",
            transactionId, request.Context.PatientId);

        // Check if this is a procedure we handle
        var procedureCode = ExtractProcedureCode(request);
        if (procedureCode is null || !SupportedProcedureCodes.Contains(procedureCode))
        {
            logger.LogInformation("Procedure code {Code} not supported, returning empty cards", procedureCode);
            return Results.Ok(new CdsResponse { Cards = [] });
        }

        // Set up timeout for CDS Hook response (Epic expects <10 seconds)
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(8));

        try
        {
            // Check cache first (for demo scenarios)
            var cacheKey = $"{request.Context.PatientId}:{procedureCode}";
            var cachedResponse = await cacheService.GetCachedResponseAsync(cacheKey, cts.Token);
            if (cachedResponse is not null)
            {
                logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                return Results.Ok(BuildSuccessCard(transactionId, cachedResponse, config));
            }

            // No access token check needed - IHttpClientProvider handles auth internally

            // 1. Aggregate FHIR data using Result pattern
            var bundleResult = await fhirAggregator.AggregateClinicalDataAsync(
                request.Context.PatientId,
                cts.Token);

            if (bundleResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to aggregate FHIR data: {Error}",
                    bundleResult.Error!.Message);
                return Results.Ok(BuildErrorCard(bundleResult.Error.Message));
            }

            // 2. Send to Intelligence service for analysis
            var analysisResult = await intelligenceClient.AnalyzeAsync(
                bundleResult.Value!,
                procedureCode,
                cts.Token);

            if (analysisResult.IsFailure)
            {
                logger.LogWarning(
                    "Intelligence analysis failed: {Error}",
                    analysisResult.Error!.Message);
                return Results.Ok(BuildFallbackCard(transactionId, config));
            }

            var formData = analysisResult.Value!;

            // 3. Stamp PDF form
            var pdfBytes = await pdfStamper.StampFormAsync(formData, cts.Token);

            // 4. Upload to Epic
            var uploadResult = await epicUploader.UploadDocumentAsync(
                pdfBytes,
                request.Context.PatientId,
                request.Context.EncounterId,
                cts.Token);

            string? documentId = null;
            if (uploadResult.IsSuccess)
            {
                documentId = uploadResult.Value;
            }
            else
            {
                logger.LogWarning(
                    "Document upload failed: {Error}. Returning card without document reference.",
                    uploadResult.Error!.Message);
            }

            // Cache the successful response for demo purposes
            await cacheService.SetCachedResponseAsync(cacheKey, formData, cts.Token);

            logger.LogInformation(
                "PA form generated successfully. TransactionId={TransactionId}, DocumentId={DocumentId}",
                transactionId, documentId);

            return Results.Ok(BuildSuccessCard(transactionId, formData, config, documentId));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Pipeline timeout for TransactionId={TransactionId}", transactionId);
            return Results.Ok(BuildProcessingCard(transactionId, config));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline error for TransactionId={TransactionId}", transactionId);
            return Results.Ok(BuildFallbackCard(transactionId, config));
        }
    }

    private static string? ExtractProcedureCode(CdsRequest request)
    {
        var entries = request.Context.DraftOrders?.Entry;
        if (entries is null) return null;

        foreach (var entry in entries)
        {
            var codings = entry.Resource?.Code?.Coding;
            if (codings is null) continue;

            foreach (var coding in codings)
            {
                if (coding.System?.Contains("cpt", StringComparison.OrdinalIgnoreCase) == true
                    || string.IsNullOrEmpty(coding.System))
                {
                    if (!string.IsNullOrEmpty(coding.Code))
                        return coding.Code;
                }
            }
        }

        return null;
    }

    private static CdsResponse BuildSuccessCard(
        string transactionId,
        PAFormData formData,
        IConfiguration config,
        string? documentId = null)
    {
        var dashboardUrl = config["Dashboard:BaseUrl"] ?? "http://localhost:5173";
        var confidencePercent = (int)(formData.ConfidenceScore * 100);

        return new CdsResponse
        {
            Cards =
            [
                new CdsCard
                {
                    Uuid = transactionId,
                    Summary = "Prior Authorization Form Ready",
                    Detail = $"AuthScript has completed the PA form for MRI Lumbar Spine. " +
                             $"Confidence: {confidencePercent}%. Recommendation: {formData.Recommendation}",
                    Indicator = formData.Recommendation == "APPROVE" ? "info" : "warning",
                    Source = new CdsSource
                    {
                        Label = "AuthScript",
                        Url = dashboardUrl
                    },
                    Suggestions = documentId is not null
                        ?
                        [
                            new CdsSuggestion
                            {
                                Label = "Review Form",
                                Uuid = $"suggestion-{transactionId}",
                                IsRecommended = true,
                                Actions =
                                [
                                    new CdsAction
                                    {
                                        Type = "create",
                                        Description = "Open completed PA form",
                                        Resource = new { resourceType = "DocumentReference", id = documentId }
                                    }
                                ]
                            }
                        ]
                        : null,
                    Links =
                    [
                        new CdsLink
                        {
                            Label = "View in AuthScript Dashboard",
                            Url = $"{dashboardUrl}/analysis/{transactionId}",
                            Type = "absolute"
                        }
                    ]
                }
            ]
        };
    }

    private static CdsResponse BuildProcessingCard(string transactionId, IConfiguration config)
    {
        var dashboardUrl = config["Dashboard:BaseUrl"] ?? "http://localhost:5173";

        return new CdsResponse
        {
            Cards =
            [
                new CdsCard
                {
                    Uuid = transactionId,
                    Summary = "Processing Prior Authorization...",
                    Detail = "AuthScript is analyzing the clinical data. Check the dashboard for real-time status.",
                    Indicator = "info",
                    Source = new CdsSource { Label = "AuthScript", Url = dashboardUrl },
                    Links =
                    [
                        new CdsLink
                        {
                            Label = "View Progress",
                            Url = $"{dashboardUrl}/analysis/{transactionId}",
                            Type = "absolute"
                        }
                    ]
                }
            ]
        };
    }

    private static CdsResponse BuildFallbackCard(string transactionId, IConfiguration config)
    {
        var dashboardUrl = config["Dashboard:BaseUrl"] ?? "http://localhost:5173";

        return new CdsResponse
        {
            Cards =
            [
                new CdsCard
                {
                    Uuid = transactionId,
                    Summary = "Launch AuthScript",
                    Detail = "Automated analysis encountered an issue. Launch AuthScript to complete the PA form manually.",
                    Indicator = "warning",
                    Source = new CdsSource { Label = "AuthScript", Url = dashboardUrl },
                    Links =
                    [
                        new CdsLink
                        {
                            Label = "Launch AuthScript App",
                            Url = $"{dashboardUrl}/smart-launch?transaction={transactionId}",
                            Type = "smart"
                        }
                    ]
                }
            ]
        };
    }

    private static CdsResponse BuildErrorCard(string message)
    {
        return new CdsResponse
        {
            Cards =
            [
                new CdsCard
                {
                    Summary = "AuthScript Error",
                    Detail = message,
                    Indicator = "critical",
                    Source = new CdsSource { Label = "AuthScript" }
                }
            ]
        };
    }
}
