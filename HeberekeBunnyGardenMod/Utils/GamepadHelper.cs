using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

#nullable enable

namespace HeberekeBunnyGardenMod.Utils;

public enum ControllerButton
{
    None,
    A,
    B,
    X,
    Y,
    L,
    R,
    ZL,
    ZR,
    Start,
    Select,
}

/// <summary>
/// 接続中の全ゲームパッドを対象にしたボタン・スティック読み取りヘルパ。
///
/// <para>
/// ホットキー判定やフリーカメラ操作から毎フレーム呼ばれるため、LINQ やクロージャを
/// 使わず <see cref="Gamepad.all"/>（ReadOnlyArray）を添字ループで走査する。
/// これにより定常時のヒープ確保をゼロにし、GC スパイクを避ける。
/// </para>
/// </summary>
public static class GamepadHelper
{
    internal static bool IsButtonHeld(ControllerButton button)
    {
        if (button == ControllerButton.ZL || button == ControllerButton.ZR)
            return ReadTrigger(button) >= Configs.ControllerTriggerDeadzone.Value;

        return IsHeld(button);
    }

    /// <summary>接続中のパッドのうち、最初に入力のある左スティック値を返す。</summary>
    internal static Vector2 ReadLeftStick()
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            Vector2 value = pads[i].leftStick.ReadValue();
            if (value.sqrMagnitude > 0f)
                return value;
        }
        return Vector2.zero;
    }

    /// <summary>接続中のパッドのうち、最初に入力のある右スティック値を返す。</summary>
    internal static Vector2 ReadRightStick()
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            Vector2 value = pads[i].rightStick.ReadValue();
            if (value.sqrMagnitude > 0f)
                return value;
        }
        return Vector2.zero;
    }

    /// <summary>接続中のパッドのうち、最初に踏み込みのあるトリガー値 (0〜1) を返す。</summary>
    internal static float ReadTrigger(ControllerButton button)
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            float value = button switch
            {
                ControllerButton.ZL => pads[i].leftTrigger.ReadValue(),
                ControllerButton.ZR => pads[i].rightTrigger.ReadValue(),
                _ => 0f,
            };
            if (value > 0f)
                return value;
        }
        return 0f;
    }

    /// <summary>いずれかのパッドでこのフレームに押されたら true。</summary>
    internal static bool IsTriggered(ControllerButton button)
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            if (GetRawGamepadButton(pads[i], button)?.wasPressedThisFrame == true)
                return true;
        }
        return false;
    }

    /// <summary>いずれかのパッドで押下中なら true。</summary>
    internal static bool IsHeld(ControllerButton button)
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            if (GetRawGamepadButton(pads[i], button)?.isPressed == true)
                return true;
        }
        return false;
    }

    private static ButtonControl? GetRawGamepadButton(Gamepad? gamepad, ControllerButton button)
    {
        if (gamepad == null)
            return null;

        return button switch
        {
            ControllerButton.A => gamepad.buttonSouth,
            ControllerButton.B => gamepad.buttonEast,
            ControllerButton.X => gamepad.buttonWest,
            ControllerButton.Y => gamepad.buttonNorth,
            ControllerButton.L => gamepad.leftShoulder,
            ControllerButton.R => gamepad.rightShoulder,
            ControllerButton.ZL => gamepad.leftTrigger,
            ControllerButton.ZR => gamepad.rightTrigger,
            ControllerButton.Start => gamepad.startButton,
            ControllerButton.Select => GetRawSelectButton(gamepad),
            _ => null,
        };
    }

    private static ButtonControl GetRawSelectButton(Gamepad gamepad)
    {
        // DualShock はタッチパッド押下を Select 相当として扱う。
        if (gamepad is DualShockGamepad dualShockGamepad)
            return dualShockGamepad.touchpadButton;

        return gamepad.selectButton;
    }
}
