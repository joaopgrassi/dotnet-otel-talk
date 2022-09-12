using API.Authorization;
using API.EF;
using API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API;

internal static class HostingExtensions
{
    private static AppSettings _appSettings = null!;
    
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
        
        // Configure OpenTelemetry Tracing
        // Exporting to a OTel collector on the default port (gRPC localhost:4317)
        builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("api"))
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
}
