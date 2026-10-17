using System.Reflection;
using LiquidSimulator;
using NUnit.Framework;
using UnityEngine;

public sealed class LiquidPhysicsTests
{
    GameObject owner;
    LiquidSimulation fluid;
    static readonly MethodInfo Step = typeof(LiquidSimulation).GetMethod("Step",BindingFlags.Instance | BindingFlags.NonPublic);
    [SetUp] public void SetUp()
    {
        owner = new GameObject("Test fluid"); fluid = owner.AddComponent<LiquidSimulation>(); fluid.ResetLiquid();
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(owner);
    void Advance(int count)
    {
        for (int i = 0; i < count; i++) Step.Invoke(fluid,new object[] {1f/120f});
    }
    void AssertContained()
    {
        Assert.That(fluid.Positions.Length,Is.EqualTo(551));
        foreach (Vector2 world in fluid.Positions)
        {
            Assert.That(float.IsNaN(world.x) || float.IsInfinity(world.x) || float.IsNaN(world.y) || float.IsInfinity(world.y),Is.False);
            Vector2 p = fluid.ToLocal(world);
            Assert.That(Mathf.Abs(p.x),Is.LessThanOrEqualTo(2.426f));
            Assert.That(Mathf.Abs(p.y),Is.LessThanOrEqualTo(2.426f));
        }
    }
    [TestCase(0f)] [TestCase(1f)]
    public void SettlingPreservesMassAndContainment(float viscosity)
    {
        fluid.Viscosity = viscosity; Advance(240); AssertContained();
    }
    [Test] public void ShakingAndTiltingPreserveContainment()
    {
        for (int cycle = 0; cycle < 8; cycle++)
        {
            float sign = cycle % 2 == 0 ? 1 : -1;
            fluid.SetTarget(new Vector2(sign*2,sign),sign*60); Advance(30); AssertContained();
        }
    }
    [Test] public void InvalidTargetsDoNotPoisonTheSimulation()
    {
        fluid.SetTarget(new Vector2(1, 1), 30); Advance(60);
        Vector2 center = fluid.ContainerPosition;
        float angle = fluid.ContainerAngle;
        fluid.SetTarget(new Vector2(float.NaN, 0), 0);
        fluid.SetTarget(new Vector2(0, float.PositiveInfinity), 0);
        fluid.SetTarget(Vector2.zero, float.NaN);
        Advance(30);
        Assert.That(fluid.ContainerPosition, Is.EqualTo(center));
        Assert.That(fluid.ContainerAngle, Is.EqualTo(angle));
        AssertContained();
    }
    [Test] public void ResetRecreatesFluidInsideMovedContainer()
    {
        fluid.SetTarget(new Vector2(2,1),40); Advance(60);
        fluid.ResetLiquid(); AssertContained();
    }
}
