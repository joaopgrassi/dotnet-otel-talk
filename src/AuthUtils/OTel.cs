using System.Diagnostics;

namespace AuthUtils;

public class OTel
{
    public const string TracerName = "AuthUtils";
    
    public static ActivitySource Tracer = new ActivitySource(TracerName, "0.0.1");
}
