using System.Diagnostics;

namespace API;

public class OTel
{
    public const string TracerName = "API";
    
    public static ActivitySource Tracer = new ActivitySource(TracerName, "0.0.1");
}
