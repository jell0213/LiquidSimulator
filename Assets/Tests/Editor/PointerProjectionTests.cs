using LiquidSimulator;
using NUnit.Framework;
using UnityEngine;

public sealed class PointerProjectionTests
{
    GameObject owner;
    Camera camera;
    [SetUp] public void SetUp()
    {
        owner = new GameObject("Projection camera"); camera = owner.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.pixelRect = new Rect(0, 0, 640, 480);
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(owner);
    [TestCase(float.NaN, 240f)]
    [TestCase(320f, float.NaN)]
    [TestCase(float.PositiveInfinity, 240f)]
    [TestCase(320f, float.NegativeInfinity)]
    [TestCase(-1f, 240f)]
    [TestCase(640f, 240f)]
    public void InvalidPointerIsRejectedBeforeProjection(float x, float y)
    {
        Assert.That(PointerProjection.TryGetWorldPoint(camera, new Vector3(x, y, 0), out _), Is.False);
    }
    [Test] public void CenterProjectsOntoLiquidPlane()
    {
        Assert.That(PointerProjection.TryGetWorldPoint(camera, new Vector3(320, 240, 0), out Vector2 point), Is.True);
        Assert.That(point.magnitude, Is.LessThan(0.001f));
    }
    [Test] public void MissingCameraIsRejected()
    {
        Assert.That(PointerProjection.TryGetWorldPoint(null, Vector3.zero, out _), Is.False);
    }
}
