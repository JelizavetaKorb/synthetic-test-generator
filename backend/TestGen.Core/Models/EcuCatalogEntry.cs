namespace TestGen.Core.Models;

public sealed class EcuCatalogEntry
{
    public string   EcuId                 { get; set; } = string.Empty;
    public string   EcuName               { get; set; } = string.Empty;
    public string   Manufacturer          { get; set; } = string.Empty;
    public string[] SupportedInterfaces   { get; set; } = Array.Empty<string>();
    public double   MinOperationalVoltage { get; set; }
    public double   MaxOperationalVoltage { get; set; }
    public string[] SupportedModes        { get; set; } = Array.Empty<string>();
    public string[] FirmwareVersions      { get; set; } = Array.Empty<string>();

    /// <summary>
    /// ECU functional class. One of: <c>Engine</c>, <c>Brake</c>, <c>BMS</c>,
    /// <c>ADAS</c>, or empty when not classified. Drives §6.3 domain rules
    /// (17 Brake, 18 Engine, 19 BMS).
    /// </summary>
    public string EcuClass { get; set; } = string.Empty;
}
