using API.Authorization;
using API.EF;
using API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

namespace API;

internal static class HostingExtensions
{
    private static AppSettings _appSettings = null!;
    private static readonly ResourceBuilder _otelResource = ResourceBuilder.CreateDefault().AddService("api");

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        _appSettings = builder.Configuration.ConfigureAndGet<AppSettings>(builder.Services, AppSettings.SectionName);

        builder.Services.AddControllers();
        builder.Services.AddDbContext<AuthzContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("AuthzConnection")));

        builder.Services.AddHostedService<DbMigratorHostedService>();
        builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
        builder.Services.AddSwagger(_appSettings);
        builder.Services.AddAuthentication(_appSettings);

        builder.AddSerilog();

        builder.AddOpenTelemetry();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            options.OAuthClientId(_appSettings.Swagger.ClientId);
            options.OAuthAppName(_appSettings.Swagger.ClientId);
            options.OAuthUsePkce();
        });

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();

        // order here matters - after UseAuthentication so we have the Identity populated in the HttpContext
        app.UseMiddleware<PermissionsMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        return app;
    }

    private static void AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        // Configure OpenTelemetry Tracing and Metrics
        // Exporting to a OTel collector on the default port (gRPC localhost:4317)
        builder.Services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .SetResourceBuilder(_otelResource)

                // Collect spans from both the API and AuthUtils project
                .AddSource(AuthUtils.OTel.Tracer.Name)
                .AddSource(OTel.Tracer.Name)

                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    // Don't collect spans for requests on swagger things (e.g. /swagger/index.html)
                    opts.Filter = req => !req.Request.Path.ToUriComponent().StartsWith("/swagger");
                })
                .AddEntityFrameworkCoreInstrumentation(opts =>
                {
                    opts.SetDbStatementForText = true;
                })
                .AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetryMetrics(builder =>
        {
            builder
                .SetResourceBuilder(_otelResource)
                // Collect metrics from the AuthUtils project
                .AddMeter(AuthUtils.OTel.Meter.Name)
                // Collect metrics from ASP.NET and HTTP Client calls
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter((OtlpExporterOptions exporterOptions, MetricReaderOptions readerOptions) =>
                {
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                    readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                });
        });
    }

    private static void AddSerilog(this WebApplicationBuilder builder)
    {
        // Tell the .NET infrastructure to inject the TraceId/SpanId to log records
        builder.Logging.Configure(opts => opts.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

        builder.Host.UseSerilog((ctx, config) =>
        {
            config
                .MinimumLevel.Information()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithSpan(new SpanOptions
                {
                    // allows renaming how the TraceId and SpanId fields will be named in each log record
                    LogEventPropertiesNames = new()
                    {
                        TraceId = "trace_id",
                        SpanId = "span_id"
                    }
                })
                .WriteTo.Console(new CompactJsonFormatter());
        });
    }
}
