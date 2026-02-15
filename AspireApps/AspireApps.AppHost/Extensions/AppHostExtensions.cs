using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace AspireApps.AppHost.Extensions;

/// <summary>
/// Extension methods for AppHost resource configuration.
/// </summary>
public static class AppHostExtensions
{
    /// <summary>
    /// Adds Application Insights connection string to the service if available.
    /// This reduces boilerplate code when configuring multiple services.
    /// </summary>
    /// <typeparam name="T">Resource type that supports environment variables</typeparam>
    /// <param name="builder">The resource builder</param>
    /// <param name="appInsights">Application Insights resource (can be null for local development)</param>
    /// <returns>The resource builder for chaining</returns>
    public static IResourceBuilder<T> WithAppInsights<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureApplicationInsightsResource>? appInsights) 
        where T : IResourceWithEnvironment
    {
        if (appInsights is not null)
        {
            builder.WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", 
                appInsights.Resource.ConnectionStringExpression);
        }
        return builder;
    }
}
