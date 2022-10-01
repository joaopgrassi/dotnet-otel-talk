# OpenTelemetry in .NET

This is the accompanying repository for my meetup talk on
[Modern Observability for your .NET apps with OpenTelemetry](https://www.meetup.com/dotnet-austria/events/287926629/). 

The meetup was hosted by [.NET Devs Austria](https://dotnetdevs.at/en/bundesland/wien/) (thanks for having me!).
You can find the recording on their [YouTube Channel](https://www.youtube.com/watch?v=GMEmLbUFmhQ) (don't forget to subscribe!).

The slides for the talk can be found on [Speaker Deck](https://speakerdeck.com/joaograssi/modern-observability-for-your-net-apps-with-opentelemetry).

## Sample app code

The sample consists of a "usual" ASP.NET API, protected by JWT tokens, coming from Duende IdentityServer.
The API is pretty much the same used for my blog post series on.
[ASP.NET Authorization](https://blog.joaograssi.com/series/authorization-in-asp.net-core/). 

The difference being it's now instrumented with OpenTelemetry! 🔭

Dependencies managed in `docker-compose.yaml`:

- SQL Server
- Duende IdentityServer
- OpenTelemetry Collector
- Jaeger

## Running the sample

1. Start the dependencies by running `docker-compose up` from the root of this repo
2. Start the ASP.NET API inside `src/API` `dotnet run`
3. The API is available on `https://localhost:7249/swagger`
4. Click on the "Authorize" button on the swagger page, select the scopes and log in either with `alice:alice` or `bob:bob`. 
5. Try the requests on the Swagger pages


## Seeing the traces metrics and logs

By default, the telemetry data (traces, metrics and logs) are exported to
the OpenTelemetry collector running on Docker.
The collector is configured to export data to the console output for all signals.
Traces are also exported to [Jaeger on http://localhost:16686](http://localhost:16686/`).
Check out the `otel-collector-config.yaml` to see how the configuration looks like.

> To learn more about the collector configuration, check out the 
> [OpenTelemetry website](https://opentelemetry.io/docs/collector/configuration/)

## Interesting bits

### OTel SDK configuration

Take a look at the method `AddOpenTelemetry` inside `src/API/HostingExtensions.cs`
to see how the OpenTelemetry SDK along with several instrumentation packages
are configured.

### Manual instrumentation

To see how to manual start a span or create metrics, take a look at either `PermissionsMiddleware`
or `src/AuthUtils/PolicyProvider/PermissionHandler.cs`.

To learn more, take a look at the [OpenTelemetry .NET docs](https://opentelemetry.io/docs/instrumentation/net/getting-started/).