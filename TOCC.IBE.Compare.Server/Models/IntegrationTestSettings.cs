namespace TOCC.IBE.Compare.Server.Models;

public class IntegrationTestSettings
{
    public bool Enabled { get; set; }
    public string V1BaseUrl { get; set; } = string.Empty;
    public string V2BaseUrl { get; set; } = string.Empty;
    public string TestCasesFile { get; set; } = string.Empty;
    public string ArtifactsFolder { get; set; } = string.Empty;
}
