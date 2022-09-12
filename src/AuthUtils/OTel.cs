using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AuthUtils;

public static class OTel
{
    public static ActivitySource Tracer = new ActivitySource("AuthUtils", "0.0.1");
    
    public static readonly Meter Meter = new("AuthUtils", "0.0.1");
}
