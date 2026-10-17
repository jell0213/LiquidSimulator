using System.Collections;
using System.Collections.Generic;
using LiquidSimulator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SandboxInteractionTests
{
    readonly HashSet<GameObject> existingRoots = new();
    LiquidSandbox sandbox;
    [UnitySetUp] public IEnumerator SetUp()
    {
        foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (root.transform.parent == null) existingRoots.Add(root);
        sandbox = new GameObject("Interaction test sandbox").AddComponent<LiquidSandbox>();
        yield return null;
        Canvas.ForceUpdateCanvases();
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (root.transform.parent == null && !existingRoots.Contains(root)) Object.Destroy(root);
        existingRoots.Clear();
        yield return null;
    }
    [UnityTest] public IEnumerator DragChangesContainerPosition()
    {
        var camera = Camera.main;
        Vector3 start = camera.WorldToScreenPoint(Vector3.zero);
        Vector3 end = camera.WorldToScreenPoint(Vector3.right);
        sandbox.HandlePointer(start, true, true, true);
        sandbox.HandlePointer(end, false, true, true);
        yield return new WaitForSeconds(0.25f);
        Assert.That(sandbox.Simulation.ContainerPosition.x, Is.GreaterThan(0.5f));
    }
    [UnityTest] public IEnumerator SliderAcceptsPointerAcrossItsFullRowAndUpdatesViscosity()
    {
        Slider slider = sandbox.ViscositySlider;
        var rect = slider.GetComponent<RectTransform>();
        // y=0.75 is outside the thin visual track but inside the expanded click target.
        Vector3 local = new Vector3(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, 0.85f),
            Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, 0.75f), 0);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
        var pointer = new PointerEventData(EventSystem.current)
        { position = screen, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Exists(hit => hit.gameObject == slider.gameObject), Is.True, "Full slider row must receive UI raycasts");
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Assert.That(slider.value, Is.GreaterThan(0.7f));
        Assert.That(sandbox.Simulation.Viscosity, Is.EqualTo(slider.value).Within(0.001f));
        Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None),
            Has.Some.Matches<Text>(label => label.text.Contains("Syrup")));
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        yield return null;
    }
    [UnityTest] public IEnumerator UIInteractionDoesNotDragContainer()
    {
        var rect = sandbox.ViscositySlider.GetComponent<RectTransform>();
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        sandbox.HandlePointer(start, true, true, true);
        sandbox.HandlePointer(start + Vector2.right * 30, false, true, true);
        yield return new WaitForSeconds(0.1f);
        Assert.That(sandbox.Simulation.ContainerPosition.magnitude, Is.LessThan(0.001f));
    }
    [Test] public void EventSystemHasOneEnabledInputModule()
    {
        int enabled = 0;
        foreach (var module in EventSystem.current.GetComponents<BaseInputModule>())
            if (module.enabled) enabled++;
        Assert.That(enabled, Is.EqualTo(1));
    }
}
