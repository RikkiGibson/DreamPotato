using System;
using System.Collections.Generic;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

using Numerics = System.Numerics;

namespace DreamPotato.MonoGame;

internal sealed class TouchOverlay
{
    private static readonly bool HideControlsWhenIdle = !OperatingSystem.IsAndroid();

    private const float InactivitySeconds = 5;
    private const float FadeSeconds = 0.35f;

    private readonly HashSet<VmuButton> _primaryPressedButtons = [];
    private readonly HashSet<VmuButton> _secondaryPressedButtons = [];

    private float _inactiveSeconds;
    private float _alpha = 1;
    private int _previousActiveTouchCount;

    internal void Update(GameTime gameTime, Game1 game)
    {
        _primaryPressedButtons.Clear();
        _secondaryPressedButtons.Clear();

        if (!PlatformServices.Current.UseTouchOverlay)
            return;

        var touches = TouchPanel.GetState();
        var activeTouchCount = CountActiveTouches(touches);
        if (HideControlsWhenIdle)
        {
            var twoFingerReveal = _previousActiveTouchCount < 2 && activeTouchCount >= 2;
            if (twoFingerReveal)
            {
                Reveal();
                _previousActiveTouchCount = activeTouchCount;
            }
            else if (activeTouchCount > 0 && _alpha > 0)
            {
                _inactiveSeconds = 0;
            }
            else
            {
                _inactiveSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            _alpha = CalculateAlpha();
        }
        else
        {
            _alpha = 1;
            _inactiveSeconds = 0;
        }

        if (_alpha > 0)
        {
            UpdatePresenterButtons(touches, game.PrimaryVmuPresenter, _primaryPressedButtons);
            if (game.SecondaryVmuPresenter is { } secondaryPresenter)
                UpdatePresenterButtons(touches, secondaryPresenter, _secondaryPressedButtons);
        }

        _previousActiveTouchCount = activeTouchCount;
    }

    internal bool IsPressed(bool isPrimary, VmuButton button)
    {
        if (!PlatformServices.Current.UseTouchOverlay || _alpha <= 0)
            return false;

        return isPrimary
            ? _primaryPressedButtons.Contains(button)
            : _secondaryPressedButtons.Contains(button);
    }

    internal Func<VmuButton, bool> CreateTouchInputFilter(bool isPrimary)
        => button => IsPressed(isPrimary, button);

    internal void Layout(Game1 game)
    {
        if (!PlatformServices.Current.UseTouchOverlay || _alpha <= 0)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        DrawPresenterButtons(drawList, game.PrimaryVmuPresenter);
        if (game.SecondaryVmuPresenter is { } secondaryPresenter)
            DrawPresenterButtons(drawList, secondaryPresenter);
    }

    private void Reveal()
    {
        _inactiveSeconds = 0;
        _alpha = 1;
    }

    private float CalculateAlpha()
    {
        if (_inactiveSeconds <= InactivitySeconds)
            return 1;

        return Math.Clamp(1 - (_inactiveSeconds - InactivitySeconds) / FadeSeconds, 0, 1);
    }

    private static int CountActiveTouches(TouchCollection touches)
    {
        var count = 0;
        foreach (var touch in touches)
        {
            if (touch.State is TouchLocationState.Pressed or TouchLocationState.Moved)
                count++;
        }

        return count;
    }

    private void UpdatePresenterButtons(TouchCollection touches, VmuPresenter presenter, HashSet<VmuButton> pressedButtons)
    {
        var buttonLayoutBounds = GetButtonLayoutBounds(presenter);
        foreach (var touch in touches)
        {
            if (touch.State is not (TouchLocationState.Pressed or TouchLocationState.Moved))
                continue;

            var point = new Point((int)touch.Position.X, (int)touch.Position.Y);
            foreach (var button in GetButtonLayout(buttonLayoutBounds))
            {
                if (button.Bounds.Contains(point))
                    pressedButtons.Add(button.Button);
            }
        }
    }

    private void DrawPresenterButtons(ImDrawListPtr drawList, VmuPresenter presenter)
    {
        foreach (var button in GetButtonLayout(GetButtonLayoutBounds(presenter)))
        {
            var color = ImGui.GetColorU32(new Numerics.Vector4(0.05f, 0.05f, 0.05f, 0.48f * _alpha));
            var borderColor = ImGui.GetColorU32(new Numerics.Vector4(1, 1, 1, 0.65f * _alpha));
            var textColor = ImGui.GetColorU32(new Numerics.Vector4(1, 1, 1, 0.8f * _alpha));
            var min = new Numerics.Vector2(button.Bounds.Left, button.Bounds.Top);
            var max = new Numerics.Vector2(button.Bounds.Right, button.Bounds.Bottom);
            drawList.AddRectFilled(min, max, color, rounding: 8);
            drawList.AddRect(min, max, borderColor, rounding: 8, flags: ImDrawFlags.None, thickness: 1.5f);

            var textSize = ImGui.CalcTextSize(button.Label);
            var textPos = new Numerics.Vector2(
                button.Bounds.Left + (button.Bounds.Width - textSize.X) / 2,
                button.Bounds.Top + (button.Bounds.Height - textSize.Y) / 2);
            drawList.AddText(textPos, textColor, button.Label);
        }
    }

    private static Rectangle GetButtonLayoutBounds(VmuPresenter presenter)
    {
#if ANDROID
        if (presenter.DrawnBounds.Width > 0 && presenter.DrawnBounds.Height > 0)
            return presenter.DrawnBounds;
#endif
        return presenter.Bounds;
    }

    private static ButtonRegion[] GetButtonLayout(Rectangle bounds)
    {
        var baseSize = Math.Min(bounds.Width, bounds.Height);
        var buttonSize = Math.Clamp(baseSize / 8, 44, 88);
        var gap = Math.Max(8, buttonSize / 7);
        var left = bounds.Left + gap * 2;
        var bottom = bounds.Bottom - gap * 2;
        var right = bounds.Right - gap * 2;
        var dpadX = left + buttonSize;
        var dpadY = bottom - buttonSize * 2;
        var actionY = bottom - buttonSize * 2;
        var centerX = (bounds.Left + bounds.Right) / 2;

        return [
            new(VmuButton.Up, new Rectangle(dpadX, dpadY - buttonSize, buttonSize, buttonSize), "U"),
            new(VmuButton.Down, new Rectangle(dpadX, dpadY + buttonSize, buttonSize, buttonSize), "D"),
            new(VmuButton.Left, new Rectangle(dpadX - buttonSize, dpadY, buttonSize, buttonSize), "L"),
            new(VmuButton.Right, new Rectangle(dpadX + buttonSize, dpadY, buttonSize, buttonSize), "R"),
            new(VmuButton.B, new Rectangle(right - buttonSize * 2 - gap, actionY + buttonSize / 2, buttonSize, buttonSize), "B"),
            new(VmuButton.A, new Rectangle(right - buttonSize, actionY, buttonSize, buttonSize), "A"),
            new(VmuButton.Mode, new Rectangle(centerX - buttonSize - gap / 2, bottom - buttonSize, buttonSize, buttonSize), "MODE"),
            new(VmuButton.Sleep, new Rectangle(centerX + gap / 2, bottom - buttonSize, buttonSize, buttonSize), "SLEEP"),
        ];
    }

    private readonly record struct ButtonRegion(VmuButton Button, Rectangle Bounds, string Label);
}
