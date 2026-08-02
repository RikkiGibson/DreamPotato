namespace ImGuiNET
{
    [System.Flags]
    public enum ImDrawFlags : uint
    {
        None = 0,
        RoundCornersTopLeft = 16,
        RoundCornersTopRight = 32,
        RoundCornersBottomLeft = 64,
        RoundCornersBottomRight = 128,
        RoundCornersNone = 256,
        Closed = 512,
        RoundCornersTop = 48,
        RoundCornersBottom = 192,
        RoundCornersLeft = 80,
        RoundCornersRight = 160,
        RoundCornersAll = 240,
        RoundCornersDefault = 240,
        RoundCornersMask = 496,
        InvalidMask = 2147483663,
    }
}
