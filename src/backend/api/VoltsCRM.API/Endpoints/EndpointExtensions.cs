namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Aggregates every resource endpoint module. <c>Program.cs</c> calls <see cref="MapApiEndpoints"/> once;
/// new resources register their <c>Map&lt;Resource&gt;Endpoints()</c> here as they are built.
/// </summary>
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapAccessEndpoints();
        app.MapPhoenixEndpoints();
        app.MapInventoryEndpoints();
        app.MapServicePlanEndpoints();
        app.MapCustomerEndpoints();
        app.MapAgentEndpoints();
        app.MapSubscriptionEndpoints();
        app.MapPaymentEndpoints();
        app.MapInvoiceEndpoints();
        app.MapDiscountEndpoints();
        app.MapInstallmentEndpoints();
        app.MapReportEndpoints();
        app.MapPortalEndpoints();
        app.MapGeocodingEndpoints();
        app.MapImportEndpoints();
        app.MapSettingsEndpoints();
        app.MapWebhookEndpoints();

        // Phase 9+ resources are added here, e.g.:
        // app.MapPaymentGatewayEndpoints();

        return app;
    }
}
