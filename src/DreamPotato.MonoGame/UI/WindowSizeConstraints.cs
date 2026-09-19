using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace DreamPotato.MonoGame;

internal enum WindowSizeConstraintKind
{
    None,
    Minimum,
    UserPreference,
    PrimaryMenuBar,
    SettingsWindow,
}

internal record struct WindowSizeConstraint(WindowSizeConstraintKind Kind, ViewportSize MinSize)
{
    public static WindowSizeConstraint SettingsWindow = new(WindowSizeConstraintKind.SettingsWindow, new(300, 300));
}

internal class WindowSizeConstraints
{
    private readonly List<WindowSizeConstraint> _constraints = [];

    public void AddOrUpdate(WindowSizeConstraint constraint)
    {
        var index = _constraints.FindIndex(c => c.Kind == constraint.Kind);
        if (index == -1)
            _constraints.Add(constraint);
        else
            _constraints[index] = constraint;
    }

    public void Remove(WindowSizeConstraintKind kind)
    {
        var indexToRemove = _constraints.FindIndex(c => c.Kind == kind);
        if (indexToRemove != -1)
            _constraints.RemoveAt(indexToRemove);
    }

    public WindowSizeConstraint GetConstraint(WindowSizeConstraintKind kind)
    {
        return _constraints.FirstOrDefault(c => c.Kind == kind);
    }

    public ViewportSize GetWindowSize()
    {
        const int MinWidth = VmuPresenter.TotalContentWidth;
        const int MinHeight = VmuPresenter.TotalContentHeight + Game1.MenuBarHeight;

        var (width, height) = (MinWidth, MinHeight);
        foreach (var constraint in _constraints)
        {
            width = Math.Max(constraint.MinSize.Width, width);
            height = Math.Max(constraint.MinSize.Height, height);
        }

        return new ViewportSize(width, height);
    }
}
