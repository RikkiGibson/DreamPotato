namespace ImGuiNET
{
    [System.Flags]
    public enum ImGuiBackendFlags
    {
        None = 0,
        HasGamepad = 1,
        HasMouseCursors = 2,
        HasSetMousePos = 4,
        RendererHasVtxOffset = 8,
        RendererHasTextures = 16,
        RendererHasViewports = 1024,
        PlatformHasViewports = 2048,
        HasMouseHoveredViewport = 4096,
        HasParentViewport = 8192,
    }
}
