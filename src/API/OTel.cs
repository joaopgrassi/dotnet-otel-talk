using System.Diagnostics;

namespace API;

public static class OTel
{
    public static readonly ActivitySource Tracer = new ActivitySource("API", "0.0.1");
}
