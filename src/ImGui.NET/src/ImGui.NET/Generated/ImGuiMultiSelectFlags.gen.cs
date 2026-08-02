namespace ImGuiNET
{
    [System.Flags]
    public enum ImGuiMultiSelectFlags
    {
        None = 0,
        SingleSelect = 1,
        NoSelectAll = 2,
        NoRangeSelect = 4,
        NoAutoSelect = 8,
        NoAutoClear = 16,
        NoAutoClearOnReselect = 32,
        BoxSelect1d = 64,
        BoxSelect2d = 128,
        BoxSelectNoScroll = 256,
        ClearOnEscape = 512,
        ClearOnClickVoid = 1024,
        ScopeWindow = 2048,
        ScopeRect = 4096,
        SelectOnAuto = 8192,
        SelectOnClickAlways = 16384,
        SelectOnClickRelease = 32768,
        NavWrapX = 65536,
        NoSelectOnRightClick = 131072,
        SelectOnMask = 57344,
    }
}
