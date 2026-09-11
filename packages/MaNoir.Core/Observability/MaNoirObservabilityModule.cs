using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MaNoir.Core.Observability;

/// <summary>
/// Exposes the standard OpenTelemetry bootstrap used by MaNoir hosts.
/// </summary>
public static class MaNoirObservabilityModule
{
    private const string DefaultMetricsPath = "/metrics";

    /// <summary>
    /// Adds the standard MaNoir OpenTelemetry wiring to a worker or agent host.
    /// </summary>
    public static IHostApplicationBuilder AddMaNoirAgentObservability(this IHostApplicationBuilder builder, string defaultServiceName)
    {
        if (!IsObservabilityEnabled())
        {
            return builder;
        }

        AddObservability(builder.Services, builder.Logging, defaultServiceName, isWebHost: false);
        return builder;
    }

    /// <summary>
    /// Adds the standard MaNoir OpenTelemetry wiring to an ASP.NET Core host.
    /// </summary>
    public static WebApplicationBuilder AddMaNoirWebObservability(this WebApplicationBuilder builder, string defaultServiceName)
    {
        if (!IsObservabilityEnabled())
        {
            return builder;
        }

        AddObservability(builder.Services, builder.Logging, defaultServiceName, isWebHost: true);
        return builder;
    }

    /// <summary>
    /// Maps the standard Prometheus scraping endpoint for ASP.NET Core hosts.
    /// </summary>
    public static WebApplication MapMaNoirWebObservability(this WebApplication app)
    {
        if (!IsObservabilityEnabled())
        {
            return app;
        }

        app.MapPrometheusScrapingEndpoint(ResolveEnvironmentValue("MANOIR_PROMETHEUS_METRICS_PATH", DefaultMetricsPath));
        return app;
    }

    private static void AddObservability(IServiceCollection services, ILoggingBuilder loggingBuilder, string defaultServiceName, bool isWebHost)
    {
        string serviceName = ResolveEnvironmentValue("OTEL_SERVICE_NAME", defaultServiceName);
        string serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
            ?? typeof(MaNoirObservabilityModule).Assembly.GetName().Version?.ToString(3)
            ?? "0.0.0";
        ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName);
        string tracesEndpoint = ResolveEnvironmentValue("MANOIR_OTEL_TRACES_ENDPOINT", null);
        string logsEndpoint = ResolveEnvironmentValue("MANOIR_OTEL_LOGS_ENDPOINT", null);
        string metricsEndpoint = ResolveEnvironmentValue("MANOIR_OTEL_METRICS_ENDPOINT", null);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                if (isWebHost)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }

                tracing.AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(tracesEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(tracesEndpoint, UriKind.Absolute);
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                if (isWebHost)
                {
                    metrics.AddAspNetCoreInstrumentation();
                    metrics.AddPrometheusExporter();
                }

                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();

                if (!isWebHost && !string.IsNullOrWhiteSpace(metricsEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(metricsEndpoint, UriKind.Absolute);
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            });

        loggingBuilder.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;

            if (!string.IsNullOrWhiteSpace(logsEndpoint))
            {
                logging.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(logsEndpoint, UriKind.Absolute);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
            }
        });
    }

    private static bool IsObservabilityEnabled()
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("MANOIR_OBSERVABILITY_ENABLED"), out bool isEnabled))
        {
            return isEnabled;
        }

        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MANOIR_OTEL_TRACES_ENDPOINT"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MANOIR_OTEL_LOGS_ENDPOINT"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MANOIR_OTEL_METRICS_ENDPOINT"));
    }

    private static string ResolveEnvironmentValue(string environmentVariableName, string defaultValue)
    {
        string configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(configuredValue) ? defaultValue : configuredValue.Trim();
    }
}