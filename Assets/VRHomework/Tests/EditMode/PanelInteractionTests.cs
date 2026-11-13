using NUnit.Framework;
using UnityEngine;
using QuestCounter;

public class PanelInteractionTests
{
    private static readonly Vector3 EyePosition = new Vector3(0f, 1.65f, 0f);

    private GameObject panelObject;
    private GameObject boardObject;
    private PanelMaterials materials;
    private CurvedButtonPanel panel;
    private CounterBoard board;

    [SetUp]
    public void SetUp()
    {
        materials = new PanelMaterials();

        boardObject = new GameObject("Test board");
        board = boardObject.AddComponent<CounterBoard>();
        board.Build(materials);

        panelObject = new GameObject("Test panel");
        panel = panelObject.AddComponent<CurvedButtonPanel>();
        panel.Build(materials, board);

        Physics.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(panelObject);
        Object.DestroyImmediate(boardObject);
        materials.Dispose();
    }

    [Test]
    public void BuildsSixHundredButtonsWithOneColliderEach()
    {
        Assert.AreEqual(600, panel.Buttons.Length);
        Assert.AreEqual(600, panelObject.GetComponentsInChildren<RayButton>().Length);
        Assert.AreEqual(600, panelObject.GetComponentsInChildren<BoxCollider>().Length);
        Assert.AreEqual(0, panel.PressedCount);
        Assert.AreEqual("0 / 600", board.CountText.text);
    }

    [Test]
    public void ButtonsLieOnACylinderAndFaceOutward()
    {
        foreach (RayButton button in panel.Buttons)
        {
            Vector3 flat = button.transform.position;
            flat.y = 0f;
            Assert.AreEqual(CurvedButtonPanel.Radius, flat.magnitude, 0.0001f);
            Assert.Greater(Vector3.Dot(button.transform.forward, flat.normalized), 0.9999f);
        }

        // 左边缘的按键比中间的离用户更近，说明面板确实是弯的。
        Assert.Less(panel.Buttons[0].transform.position.z, panel.Buttons[14].transform.position.z);
    }

    [Test]
    public void NeighbouringButtonsAreSeparatedByExactlyTheGap()
    {
        // 同一行相邻两列：弧长间隔应当等于键宽 + 缝隙；同一列相邻两行：高度差 = 键高 + 缝隙。
        float rowPitch = CurvedButtonPanel.KeyHeight + CurvedButtonPanel.Gap;
        Vector3 a = panel.Buttons[0].transform.position;
        Vector3 below = panel.Buttons[CurvedButtonPanel.Columns].transform.position;
        Assert.AreEqual(rowPitch, a.y - below.y, 0.00001f);

        float columnPitch = CurvedButtonPanel.KeyWidth + CurvedButtonPanel.Gap;
        float expectedAngle = columnPitch / CurvedButtonPanel.Radius;
        Vector3 b = panel.Buttons[1].transform.position;
        float actualAngle = Mathf.Atan2(b.x, b.z) - Mathf.Atan2(a.x, a.z);
        Assert.AreEqual(expectedAngle, actualAngle, 0.00001f);
    }

    [Test]
    public void ActivatingAButtonPressesItThenReleasesIt()
    {
        RayButton button = panel.Buttons[0];
        Vector3 colliderPosition = button.transform.position;
        Vector3 capBefore = button.Cap.position;
        Vector3 aim = (colliderPosition - EyePosition).normalized;
        RaycastHit hit;

        Assert.IsTrue(Physics.Raycast(EyePosition, aim, out hit, 5f));
        Assert.AreSame(button, hit.collider.GetComponent<RayButton>());

        button.Activate();
        Assert.IsTrue(button.IsPressed);
        Assert.AreEqual(1, panel.PressedCount);
        Assert.AreEqual("1 / 600", board.CountText.text);
        Assert.AreEqual(materials.Pressed, button.Cap.GetComponent<Renderer>().sharedMaterial);
        // 键帽沿按键自身的法线（远离用户）后移 0.025 米，碰撞体不动。
        Vector3 expected = capBefore + button.transform.forward * 0.025f;
        Assert.Less(Vector3.Distance(expected, button.Cap.position), 0.00001f);
        Assert.AreEqual(colliderPosition, button.transform.position);

        // 凹陷之后射线仍能命中同一个按键，才可以再点一次松开。
        Physics.SyncTransforms();
        Assert.IsTrue(Physics.Raycast(EyePosition, aim, out hit, 5f));
        Assert.AreSame(button, hit.collider.GetComponent<RayButton>());

        button.Activate();
        Assert.IsFalse(button.IsPressed);
        Assert.AreEqual(0, panel.PressedCount);
        Assert.AreEqual("0 / 600", board.CountText.text);
        Assert.AreEqual(capBefore, button.Cap.position);
        Assert.AreEqual(materials.Released, button.Cap.GetComponent<Renderer>().sharedMaterial);
    }

    [Test]
    public void CountFollowsEveryToggle()
    {
        foreach (RayButton button in panel.Buttons)
        {
            button.Activate();
        }
        Assert.AreEqual(600, panel.PressedCount);
        Assert.AreEqual("600 / 600", board.CountText.text);

        panel.Buttons[10].Activate();
        panel.Buttons[11].Activate();
        Assert.AreEqual(598, panel.PressedCount);
        Assert.AreEqual("598 / 600", board.CountText.text);
    }

    [Test]
    public void ResetAllReleasesEveryButtonAndZeroesTheCount()
    {
        panel.Buttons[0].Activate();
        panel.Buttons[599].Activate();

        panel.ResetAll();

        Assert.AreEqual(0, panel.PressedCount);
        Assert.AreEqual("0 / 600", board.CountText.text);
        foreach (RayButton button in panel.Buttons)
        {
            Assert.IsFalse(button.IsPressed);
            Assert.AreEqual(Vector3.zero, button.Cap.localPosition);
            Assert.AreEqual(materials.Released, button.Cap.GetComponent<Renderer>().sharedMaterial);
        }
    }

    [Test]
    public void PointerStopsAtAWallInsteadOfSelectingThroughIt()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.transform.position = new Vector3(0f, 1.65f, 1f);
            wall.transform.localScale = new Vector3(5f, 5f, 0.1f);
            Physics.SyncTransforms();

            RayTarget target;
            Vector3 end;
            bool canSelect = RayPointer.TryGetTarget(new Ray(EyePosition, Vector3.forward), 5f, out target, out end);

            Assert.IsFalse(canSelect);
            Assert.IsNull(target);
            Assert.Less(end.z, 1.1f);
        }
        finally
        {
            Object.DestroyImmediate(wall);
        }
    }
}
