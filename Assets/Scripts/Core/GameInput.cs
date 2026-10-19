using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Capa única de entrada. Funciona con el Input System nuevo, el antiguo o ambos,
/// así que no importa qué "Active Input Handling" tenga el proyecto.
public static class GameInput
{
    public static Vector2 Move
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1f : 0f) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1f : 0f);
            float y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1f : 0f);
            return new Vector2(x, y);
#else
            float x = ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0f) - ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1f : 0f);
            float y = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f) - ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0f);
            return new Vector2(x, y);
#endif
        }
    }

    /// Movimiento del ratón (grados aproximados) para este frame.
    public static Vector2 Look
    {
        get
        {
            Vector2 v;
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            v = m != null ? m.delta.ReadValue() * 0.08f : Vector2.zero;
#else
            v = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2f;
#endif
            return Vector2.ClampMagnitude(v, 30f);
        }
    }

    public static bool RunHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }
    }

    public static bool InteractPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);
#else
            return Input.GetKeyDown(KeyCode.E) || (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked);
#endif
        }
    }

    public static bool PausePressed { get { return Pressed(KeyName.Escape); } }
    public static bool RestartPressed { get { return Pressed(KeyName.R); } }
    /// V: cambiar entre primera y tercera persona.
    public static bool ViewTogglePressed { get { return Pressed(KeyName.V); } }
    /// F1 (solo Editor / Development Build): activa todas las anomalías elegidas.
    public static bool DebugActivatePressed { get { return Pressed(KeyName.F1); } }
    /// F2 (solo Editor / Development Build): muestra qué anomalías hay en esta partida.
    public static bool DebugListPressed { get { return Pressed(KeyName.F2); } }

    private enum KeyName { Escape, R, V, F1, F2 }

    private static bool Pressed(KeyName key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        switch (key)
        {
            case KeyName.Escape: return k.escapeKey.wasPressedThisFrame;
            case KeyName.R: return k.rKey.wasPressedThisFrame;
            case KeyName.V: return k.vKey.wasPressedThisFrame;
            case KeyName.F1: return k.f1Key.wasPressedThisFrame;
            default: return k.f2Key.wasPressedThisFrame;
        }
#else
        switch (key)
        {
            case KeyName.Escape: return Input.GetKeyDown(KeyCode.Escape);
            case KeyName.R: return Input.GetKeyDown(KeyCode.R);
            case KeyName.V: return Input.GetKeyDown(KeyCode.V);
            case KeyName.F1: return Input.GetKeyDown(KeyCode.F1);
            default: return Input.GetKeyDown(KeyCode.F2);
        }
#endif
    }
}
