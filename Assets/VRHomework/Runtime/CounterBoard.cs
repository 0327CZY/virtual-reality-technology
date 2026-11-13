using UnityEngine;

namespace QuestCounter
{
    /// <summary>
    /// 按键旁边的计数板：显示当前按下的按键总数，以及最近一次操作。
    /// 文字使用英文，因为 Unity 内置字体不含中文字形。
    /// </summary>
    public sealed class CounterBoard : MonoBehaviour
    {
        private static readonly Color LightBlue = new Color(0.6f, 0.8f, 0.95f);
        private static readonly Color Green = new Color(0.4f, 1f, 0.6f);

        private TextMesh countText;
        private TextMesh lastActionText;

        public TextMesh CountText { get { return countText; } }

        public void Build(PanelMaterials materials)
        {
            // 外壳是一块薄板，文字贴在朝向用户的一侧（本地 -Z 方向）。
            ScenePrimitives.Box("Housing", transform, Vector3.zero, new Vector3(0.54f, 0.77f, 0.04f), materials.Shell);

            float frontZ = -0.026f;
            ScenePrimitives.Label("Heading", transform, new Vector3(0f, 0.27f, frontZ), "PRESSED", 0.065f, Color.white);
            countText = ScenePrimitives.Label("Live pressed count", transform, new Vector3(0f, 0.12f, frontZ), "0 / 0", 0.09f, Green);
            lastActionText = ScenePrimitives.Label("Last action", transform, new Vector3(0f, -0.06f, frontZ), "READY", 0.034f, Color.white);
            ScenePrimitives.Label(
                "Input hint", transform, new Vector3(0f, -0.27f, frontZ),
                "AIM + TRIGGER\nPRESS AGAIN TO RELEASE", 0.026f, LightBlue);
        }

        public void Show(int pressedCount, int totalCount, string lastAction)
        {
            countText.text = pressedCount + " / " + totalCount;
            lastActionText.text = lastAction;
        }
    }
}
