using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LiquidSimulator
{
    public sealed class LiquidSandbox : MonoBehaviour
    {
        LiquidSimulation simulation;
        Camera view;
        LineRenderer outline;
        Material outlineMaterial;
        Vector2 target, dragOffset;
        float angle;
        bool dragging;
        Text viscosityLabel;
        Font font;
        readonly List<RaycastResult> uiHits = new();
        public LiquidSimulation Simulation => simulation;
        public Slider ViscositySlider { get; private set; }
        void Awake()
        {
            view = Camera.main;
            if (view == null) { var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera"; view = cameraObject.GetComponent<Camera>(); }
            view.orthographic = true; view.orthographicSize = 5.8f; view.transform.SetPositionAndRotation(new Vector3(0,0,-10), Quaternion.identity);
            view.backgroundColor = new Color(0.025f,0.045f,0.09f); view.clearFlags = CameraClearFlags.SolidColor;
            simulation = new GameObject("Fluid solver • 551 particles").AddComponent<LiquidSimulation>();
            var liquid = new GameObject("Continuous liquid surface").AddComponent<LiquidSurface>(); liquid.Simulation = simulation;
            outline = new GameObject("Square container").AddComponent<LineRenderer>();
            outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            outline.sharedMaterial = outlineMaterial; outline.useWorldSpace = false; outline.loop = true; outline.positionCount = 4;
            outline.SetPositions(new[] {new Vector3(-2.56f,-2.56f),new Vector3(2.56f,-2.56f),new Vector3(2.56f,2.56f),new Vector3(-2.56f,2.56f)});
            outline.startWidth = outline.endWidth = 0.12f; outline.startColor = outline.endColor = new Color(0.65f,0.83f,0.95f); outline.sortingOrder = 5;
            BuildUI();
        }
        void Update()
        {
            // Set the camera up before projection, and never divide by a zero/invalid aspect ratio.
            if (view == null) { dragging = false; return; }
            float aspect = view.aspect;
            if (PointerProjection.IsFinite(aspect) && aspect > 0)
            {
                float size = Mathf.Max(5.8f, 5.4f / aspect);
                if (PointerProjection.IsFinite(size)) view.orthographicSize = size;
            }
            SandboxInput.Frame input = SandboxInput.Read();
            HandlePointer(input.Position, input.Pressed, input.Held, input.HasPointer && Application.isFocused);
            float tilt = input.Tilt;
            float scroll = input.Scroll;
            if (dragging && PointerProjection.IsFinite(scroll)) tilt += scroll * 8;
            angle = Mathf.Clamp(angle + tilt * Time.deltaTime * 70, -65, 65);
            simulation.SetTarget(target, angle);
        }
        public void HandlePointer(Vector2 screen, bool pressed, bool held, bool available)
        {
            Vector2 mouse = default;
            bool valid = available && PointerProjection.TryGetWorldPoint(view, screen, out mouse);
            if (!valid || !held) { dragging = false; return; }
            if (pressed && !PointerOverUI(screen))
            {
                Vector2 local = simulation.ToLocal(mouse);
                if (Mathf.Abs(local.x) < 2.8f && Mathf.Abs(local.y) < 2.8f)
                { dragging = true; dragOffset = simulation.ContainerPosition - mouse; }
            }
            if (dragging) target = Vector2.ClampMagnitude(mouse + dragOffset, 3.5f);
        }
        bool PointerOverUI(Vector2 screen)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            // Query this pointer now instead of relying on last frame's cached pointer-over state.
            var pointer = new PointerEventData(events) { position = screen };
            uiHits.Clear(); events.RaycastAll(pointer, uiHits);
            return uiHits.Count > 0;
        }
        void LateUpdate() => outline.transform.SetPositionAndRotation(simulation.ContainerPosition, Quaternion.Euler(0,0,simulation.ContainerAngle));
        void OnApplicationFocus(bool focus) { if (!focus) dragging = false; }
        void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Settings Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,800); scaler.matchWidthOrHeight = 0.5f;
            var events = EventSystem.current;
            if (events == null) events = FindFirstObjectByType<EventSystem>();
            if (events == null) events = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            SandboxInput.ConfigureUI(events);
            var panel = UIObject("Settings", canvasObject.transform, new Vector2(20,-20), new Vector2(310,330));
            panel.AddComponent<Image>().color = new Color(0.055f,0.085f,0.15f,0.95f);
            Label(panel.transform,"LIQUID LAB",new Vector2(20,-18),new Vector2(275,38),28);
            Label(panel.transform,$"Left-drag box to stir • Q / E: tilt\nDrag + wheel: tilt\nInput: {SandboxInput.BackendName}",new Vector2(20,-64),new Vector2(275,62),15);
            viscosityLabel = Label(panel.transform,"",new Vector2(20,-132),new Vector2(275,30),19);
            var sliderObject = UIObject("Viscosity",panel.transform,new Vector2(24,-172),new Vector2(260,36));
            // The whole row is a hit target, not only a six-pixel track or the handle.
            var hitArea = sliderObject.AddComponent<Image>(); hitArea.color = Color.clear;
            var slider = sliderObject.AddComponent<Slider>(); ViscositySlider = slider;
            slider.minValue = 0; slider.maxValue = 1; slider.targetGraphic = hitArea;
            var track = UIObject("Track",sliderObject.transform,new Vector2(10,-15),new Vector2(240,6));
            var trackImage = track.AddComponent<Image>(); trackImage.color = new Color(0.18f,0.3f,0.4f); trackImage.raycastTarget = false;
            var handleArea = UIObject("Handle Area",sliderObject.transform,Vector2.zero,Vector2.zero);
            var area = handleArea.GetComponent<RectTransform>();
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(10,0); area.offsetMax = new Vector2(-10,0);
            var handle = UIObject("Handle",handleArea.transform,Vector2.zero,new Vector2(20,24));
            var handleRect = handle.GetComponent<RectTransform>(); handleRect.pivot = new Vector2(0.5f,0.5f);
            handleRect.anchorMin = new Vector2(0,0.5f); handleRect.anchorMax = new Vector2(0,0.5f);
            var handleImage = handle.AddComponent<Image>(); handleImage.color = new Color(0.25f,0.85f,1); handleImage.raycastTarget = false;
            slider.handleRect = handleRect; slider.direction = Slider.Direction.LeftToRight;
            // Slider stretches the handle vertically; subtract padding to retain a 24px visual height.
            handleRect.sizeDelta = new Vector2(20,-12);
            slider.onValueChanged.AddListener(SetViscosity);
            slider.SetValueWithoutNotify(simulation.Viscosity);
            SetViscosity(simulation.Viscosity);
            Label(panel.transform,"Water                      Syrup",new Vector2(24,-208),new Vector2(265,24),15);
            Button(panel.transform,"Reset",new Vector2(20,-260),() => { target = Vector2.zero; angle = 0; simulation.ResetLiquid(); });
            Button(panel.transform,"Pause / Play",new Vector2(160,-260),() => simulation.Paused = !simulation.Paused);
        }
        void SetViscosity(float value)
        {
            simulation.Viscosity = value;
            viscosityLabel.text = $"Viscosity  {simulation.Viscosity:0.00}  ·  {(value < 0.35f ? "Water" : value < 0.7f ? "Oil" : "Syrup")}";
        }
        GameObject UIObject(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1); rect.anchoredPosition = position; rect.sizeDelta = size; return obj;
        }
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var obj = UIObject("Label",parent,position,size); var label = obj.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = fontSize; label.color = new Color(0.85f,0.94f,1); label.raycastTarget = false; return label;
        }
        void Button(Transform parent,string title,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var obj = UIObject(title,parent,position,new Vector2(130,42)); obj.AddComponent<Image>().color = new Color(0.12f,0.3f,0.43f);
            obj.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
            var label = Label(obj.transform,title,new Vector2(0,0),new Vector2(130,42),17); label.alignment = TextAnchor.MiddleCenter;
        }
        void OnDestroy() { if (outlineMaterial != null) Destroy(outlineMaterial); }
    }
}
