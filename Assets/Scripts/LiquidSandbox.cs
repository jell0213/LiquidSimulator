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
        void Awake()
        {
            view = Camera.main;
            if (view == null) { var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera"; view = cameraObject.GetComponent<Camera>(); }
            view.orthographic = true; view.orthographicSize = 5.8f; view.transform.position = new Vector3(0,0,-10);
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
            Vector2 mouse = view.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
                Mathf.Abs(simulation.ToLocal(mouse).x) < 2.8f && Mathf.Abs(simulation.ToLocal(mouse).y) < 2.8f)
            { dragging = true; dragOffset = simulation.ContainerPosition - mouse; }
            if (Input.GetMouseButtonUp(0) || !Application.isFocused) dragging = false;
            if (dragging) target = Vector2.ClampMagnitude(mouse + dragOffset, 3.5f);
            float tilt = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
            if (dragging) tilt += Input.mouseScrollDelta.y * 8;
            angle = Mathf.Clamp(angle + tilt * Time.deltaTime * 70, -65, 65);
            simulation.SetTarget(target, angle);
            // Reserve world space for the Canvas panel on wide screens and for the container on narrow ones.
            view.orthographicSize = Mathf.Max(5.8f, 5.4f / view.aspect);
        }
        void LateUpdate() => outline.transform.SetPositionAndRotation(simulation.ContainerPosition, Quaternion.Euler(0,0,simulation.ContainerAngle));
        void OnApplicationFocus(bool focus) { if (!focus) dragging = false; }
        void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Settings Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,800); scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var panel = UIObject("Settings", canvasObject.transform, new Vector2(20,-20), new Vector2(310,330));
            panel.AddComponent<Image>().color = new Color(0.055f,0.085f,0.15f,0.95f);
            Label(panel.transform,"LIQUID LAB",new Vector2(20,-18),new Vector2(275,38),28);
            Label(panel.transform,"Drag the box to stir\nQ / E: tilt   •   drag + wheel: tilt",new Vector2(20,-64),new Vector2(275,55),16);
            viscosityLabel = Label(panel.transform,"",new Vector2(20,-132),new Vector2(275,30),19);
            var sliderObject = UIObject("Viscosity",panel.transform,new Vector2(24,-178),new Vector2(260,24));
            var slider = sliderObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            var track = UIObject("Track",sliderObject.transform,new Vector2(0,-9),new Vector2(260,6)); track.AddComponent<Image>().color = new Color(0.18f,0.3f,0.4f);
            var handleArea = UIObject("Handle Area",sliderObject.transform,Vector2.zero,new Vector2(260,24));
            var handle = UIObject("Handle",handleArea.transform,Vector2.zero,new Vector2(20,24)); var handleImage = handle.AddComponent<Image>(); handleImage.color = new Color(0.25f,0.85f,1);
            slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handleImage; slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(v => { simulation.Viscosity = v; viscosityLabel.text = $"Viscosity  {v:0.00}  ·  {(v < 0.35f ? "Water" : v < 0.7f ? "Oil" : "Syrup")}"; });
            slider.value = 0.25f;
            Label(panel.transform,"Water                      Syrup",new Vector2(24,-208),new Vector2(265,24),15);
            Button(panel.transform,"Reset",new Vector2(20,-260),() => { target = Vector2.zero; angle = 0; simulation.ResetLiquid(); });
            Button(panel.transform,"Pause / Play",new Vector2(160,-260),() => simulation.Paused = !simulation.Paused);
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
