using UnityEngine;

namespace LiquidSimulator
{
    public static class PointerProjection
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        // Unity can report an invalid pointer while Game View loses focus or is being resized.
        // Validate before calling any camera projection API; discard the sample rather than moving to (0,0).
        public static bool TryGetWorldPoint(Camera camera, Vector3 screen, out Vector2 world)
        {
            world = default;
            if (camera == null || !IsFinite(screen)) return false;
            Rect rect = camera.pixelRect;
            if (!IsFinite(rect.x) || !IsFinite(rect.y) || !IsFinite(rect.width) || !IsFinite(rect.height) ||
                rect.width <= 0 || rect.height <= 0 || !rect.Contains(new Vector2(screen.x, screen.y))) return false;
            if (!IsFinite(camera.aspect) || camera.aspect <= 0 || !IsFinite(camera.orthographicSize) ||
                camera.orthographicSize <= 0 || !IsFinite(camera.transform.position)) return false;
            Matrix4x4 projection = camera.projectionMatrix;
            Matrix4x4 view = camera.worldToCameraMatrix;
            for (int i = 0; i < 16; i++)
                if (!IsFinite(projection[i]) || !IsFinite(view[i])) return false;
            Ray ray = camera.ScreenPointToRay(screen);
            if (!IsFinite(ray.origin) || !IsFinite(ray.direction)) return false;
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float distance) || !IsFinite(distance)) return false;
            Vector3 point = ray.GetPoint(distance);
            if (!IsFinite(point)) return false;
            world = new Vector2(point.x, point.y);
            return true;
        }
    }
}
