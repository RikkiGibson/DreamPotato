namespace DreamPotato.Core;

/// <summary>Possible file formats for storing VMU files on the host file system.</summary>
public enum FileFormat
{
    /// <summary>
    /// .vmi+.vms file pair.
    /// https://vmu.falcogirgis.net/formats.html#formats_vmi
    /// https://vmu.falcogirgis.net/formats.html#formats_vms
    /// </summary>
    VmiVms,

    /// <summary>https://vmu.falcogirgis.net/formats.html#formats_dci</summary>
    Dci,
}

public static class FileFormatExtensions
{
    public static string[] Names { get; } = ["vmi/vms", "dci"];
}