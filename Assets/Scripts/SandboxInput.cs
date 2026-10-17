using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LiquidSimulator
{
    // World interaction and uGUI must use the same enabled input backend.
    public static class SandboxInput
    {
        public static string BackendName
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return "Input System";
#elif ENABLE_LEGACY_INPUT_MANAGER
                return "Input Manager";
#else
                return "No input backend enabled";
#endif
            }
        }
        public struct Frame
        {
            public Vector2 Position;
            public bool HasPointer, Pressed, Held;
            public float Tilt, Scroll;
        }
        public static Frame Read()
        {
            var frame = new Frame();
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                frame.Position = mouse.position.ReadValue();
                frame.HasPointer = PointerProjection.IsFinite(frame.Position);
                frame.Pressed = mouse.leftButton.wasPressedThisFrame;
                frame.Held = mouse.leftButton.isPressed;
                // Input System wheel values are usually 120 units per notch.
                frame.Scroll = mouse.scroll.ReadValue().y / 120f;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null)
                frame.Tilt = (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0);
#elif ENABLE_LEGACY_INPUT_MANAGER
            frame.Position = Input.mousePosition;
            frame.HasPointer = Input.mousePresent && PointerProjection.IsFinite(frame.Position);
            frame.Pressed = Input.GetMouseButtonDown(0);
            frame.Held = Input.GetMouseButton(0);
            frame.Scroll = Input.mouseScrollDelta.y;
            frame.Tilt = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
#endif
            return frame;
        }
        public static void ConfigureUI(EventSystem events)
        {
#if ENABLE_INPUT_SYSTEM
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
            var module = events.GetComponent<StandaloneInputModule>();
            if (module == null) module = events.gameObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogError("Liquid Lab requires Input System or Input Manager. Enable one in Player Settings and restart Unity.");
            return;
#endif
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            foreach (var other in events.GetComponents<BaseInputModule>()) other.enabled = other == module;
            module.enabled = true;
            events.enabled = true;
#endif
        }
    }
}
