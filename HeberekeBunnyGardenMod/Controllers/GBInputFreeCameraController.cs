using UnityEngine;
using UnityEngine.InputSystem;

namespace HeberekeBunnyGardenMod.Controllers;

public class GBInputFreeCameraController : MonoBehaviour
{
    private float rotationH;
    private float rotationV;
    private bool useMouseView = true;

    private void Start()
    {
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        rotationH = eulerAngles.y;
        rotationV = eulerAngles.x;

        if (rotationV > 180f)
        {
            rotationV -= 360f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 固定フリーカメラモードの場合は操作しない
        if (Plugin.isFixedFreeCam)
        {
            return;
        }
        // Input Systemから直接取得（GBInput経由ではない）
        if (useMouseView && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float sensitivity = Plugin.ConfigSensitivity.Value;
            rotationH += mouseDelta.x * sensitivity * Time.deltaTime;
            rotationV -= mouseDelta.y * sensitivity * Time.deltaTime;
        }

        rotationV = Mathf.Clamp(rotationV, -90f, 90f);
        transform.rotation = Quaternion.AngleAxis(rotationH, Vector3.up);
        transform.rotation *= Quaternion.AngleAxis(rotationV, Vector3.right);

        float speed = Plugin.ConfigSpeed.Value;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            {
                speed = Plugin.ConfigFastSpeed.Value;
            }
            else if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                speed = Plugin.ConfigSlowSpeed.Value;
            }

            // Input Systemから直接キーボード入力を取得
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                transform.position += speed * Time.deltaTime * transform.forward;
            }
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                transform.position -= speed * Time.deltaTime * transform.forward;
            }
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                transform.position -= speed * Time.deltaTime * transform.right;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                transform.position += speed * Time.deltaTime * transform.right;
            }

            if (Keyboard.current.qKey.isPressed)
            {
                transform.position += speed * Time.deltaTime * Vector3.up;
            }
            if (Keyboard.current.eKey.isPressed)
            {
                transform.position += speed * Time.deltaTime * Vector3.down;
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                useMouseView = !useMouseView;
                Cursor.lockState = useMouseView ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !useMouseView;
            }
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
