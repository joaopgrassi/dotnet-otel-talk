using API.Authorization;
using API.EF;
using API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using System.Diagnostics;

namespace API;

internal static class HostingExtensions
{
    private static AppSettings _appSettings = null!;
    private static readonly ResourceBuilder _otelResource = ResourceBuilder.CreateDefault()
        .AddService("api")
        .AddTelemetrySdk()
        .AddAttributes(new TagList { { "my_log_attr", "res_value" } });

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

        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Adds OpenTelemetry Traces, Metrics and Logs.
    /// Exporting to a OTel collector on the default port (gRPC localhost:4317)
    /// </summary>
    /// <param name="builder"></param>
    private static void AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Services.AddOpenTelemetry()
            .WithTracing(options =>
            {
                options
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
            })
            .WithMetrics(options =>
            {
                options
                    .SetResourceBuilder(_otelResource)
                    // Collect metrics from the AuthUtils project
                    .AddMeter("AuthUtils")
                    // Collect metrics from ASP.NET and HTTP Client calls
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter((OtlpExporterOptions exporterOptions, MetricReaderOptions readerOptions) =>
                    {
                        readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                        readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                    });
            });

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(_otelResource)
                .AddOtlpExporter() // export to the OTel collector
                .AddConsoleExporter(); // export to the console;
        });

        builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.IncludeFormattedMessage = true;
        });
    }
}
