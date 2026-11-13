using NUnit.Framework;
using QuestCounter;

public class TriggerEdgeTests
{
    private const bool XR = true;
    private const bool Available = true;

    [Test]
    public void FirstPressFiresOnlyAfterARelease()
    {
        TriggerEdge edge = new TriggerEdge();
        // 开局就按着：必须先松开才算数。
        Assert.IsFalse(edge.Evaluate(true, Available, XR));
        Assert.IsFalse(edge.Evaluate(false, Available, XR));
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
    }

    [Test]
    public void HoldingDoesNotRepeat()
    {
        TriggerEdge edge = new TriggerEdge();
        edge.Evaluate(false, Available, XR);
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
        for (int i = 0; i < 30; i++)
        {
            Assert.IsFalse(edge.Evaluate(true, Available, XR));
        }
        // 松开再按才会有第二次。
        Assert.IsFalse(edge.Evaluate(false, Available, XR));
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
    }

    [Test]
    public void LosingAndRegainingInputWhileHeldDoesNotFire()
    {
        TriggerEdge edge = new TriggerEdge();
        edge.Evaluate(false, Available, XR);
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
        // 追踪丢失（不可用），期间一直按着。
        Assert.IsFalse(edge.Evaluate(true, false, XR));
        // 恢复时扳机仍按着：不能误触。
        Assert.IsFalse(edge.Evaluate(true, Available, XR));
        // 松开再按才触发。
        Assert.IsFalse(edge.Evaluate(false, Available, XR));
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
    }

    [Test]
    public void SwitchingInputSourceWhileHeldDoesNotFire()
    {
        TriggerEdge edge = new TriggerEdge();
        // 鼠标模式，正常点一次。
        edge.Evaluate(false, Available, false);
        Assert.IsTrue(edge.Evaluate(true, Available, false));
        edge.Evaluate(false, Available, false);
        // 切到手柄，而扳机此刻已按住：不能当成新点击。
        Assert.IsFalse(edge.Evaluate(true, Available, XR));
        // 要先松开再扣。
        Assert.IsFalse(edge.Evaluate(false, Available, XR));
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
    }

    [Test]
    public void ReleaseWhileUnavailableStillRequiresAFreshPress()
    {
        TriggerEdge edge = new TriggerEdge();
        edge.Evaluate(false, Available, XR);
        Assert.IsTrue(edge.Evaluate(true, Available, XR));
        // 不可用期间松开。
        Assert.IsFalse(edge.Evaluate(false, false, XR));
        // 恢复后第一帧就按下：重新武装那一帧忽略，不触发。
        Assert.IsFalse(edge.Evaluate(true, Available, XR));
    }
}
