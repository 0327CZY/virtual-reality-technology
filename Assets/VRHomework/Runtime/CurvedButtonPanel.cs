using UnityEngine;

namespace QuestCounter
{
    /// <summary>
    /// 在圆柱面上生成 30 列 x 20 行的按键，并统计当前按下的数量。
    /// 圆柱的轴是穿过用户的竖直线，所以每个按键到用户的水平距离都等于 Radius。
    /// </summary>
    public sealed class CurvedButtonPanel : MonoBehaviour
    {
        public const int Columns = 30;
        public const int Rows = 20;
        public const int TotalCount = Columns * Rows;

        // 曲面和按键尺寸，单位都是米。
        public const float Radius = 2f;
        public const float CenterHeight = 1.65f;   // 面板中心高度，约等于眼睛高度
        public const float KeyWidth = 0.064f;
        public const float KeyHeight = 0.052f;
        public const float KeyThickness = 0.024f;
        public const float Gap = 0.002f;

        // 底座比键帽略大、略靠后。按下的键帽会沉进底座，看起来是凹下去的。
        private static readonly Vector3 SocketOffset = new Vector3(0f, 0f, 0.055f);
        private static readonly Vector3 SocketSize = new Vector3(0.065f, 0.053f, 0.08f);

        private PanelMaterials materials;
        private CounterBoard counter;

        public RayButton[] Buttons { get; private set; }
        public int PressedCount { get; private set; }

        public void Build(PanelMaterials palette, CounterBoard board)
        {
            materials = palette;
            counter = board;
            Buttons = new RayButton[TotalCount];

            float columnPitch = KeyWidth + Gap;
            float rowPitch = KeyHeight + Gap;

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    // 列沿圆弧均匀排开。正中间（第 14.5 列）角度为 0，
                    // 相邻两列的弧长间隔 = 键宽 + 缝隙，所以按键紧挨着但不重叠。
                    float angle = (column - (Columns - 1) / 2f) * columnPitch / Radius;

                    // 行在竖直方向排开，第 0 行在最上面。
                    float y = CenterHeight + ((Rows - 1) / 2f - row) * rowPitch;

                    Vector3 position = new Vector3(Radius * Mathf.Sin(angle), y, Radius * Mathf.Cos(angle));

                    // 绕 Y 轴转 angle 度，让按键的 +Z 指向圆柱外侧。
                    Quaternion rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

                    Buttons[row * Columns + column] = CreateButton(row, column, position, rotation);
                }
            }

            PressedCount = 0;
            counter.Show(PressedCount, TotalCount, "READY");
        }

        private RayButton CreateButton(int row, int column, Vector3 position, Quaternion rotation)
        {
            GameObject anchor = new GameObject(string.Format("Key R{0:00} C{1:00}", row + 1, column + 1));
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = position;
            anchor.transform.localRotation = rotation;

            // 碰撞体固定在原位，只有键帽会移动。
            // 这样按下后射线仍然能命中这个按键，才能再次点击把它松开。
            BoxCollider hitBox = anchor.AddComponent<BoxCollider>();
            hitBox.size = new Vector3(KeyWidth, KeyHeight, KeyThickness);

            ScenePrimitives.Box("Socket", anchor.transform, SocketOffset, SocketSize, materials.Shell);
            GameObject cap = ScenePrimitives.Box(
                "Moving cap", anchor.transform, Vector3.zero,
                new Vector3(KeyWidth, KeyHeight, KeyThickness), materials.Released);

            RayButton button = anchor.AddComponent<RayButton>();
            button.Setup(this, row, column, cap.transform, materials);
            return button;
        }

        // 某个按键切换状态后由它调用：按下 +1，松开 -1，然后刷新计数板。
        public void OnButtonChanged(RayButton button)
        {
            PressedCount += button.IsPressed ? 1 : -1;

            string state = button.IsPressed ? "DOWN" : "UP";
            string lastAction = string.Format("R{0:00} C{1:00} / {2}", button.Row + 1, button.Column + 1, state);
            counter.Show(PressedCount, TotalCount, lastAction);
        }

        public void ResetAll()
        {
            if (Buttons == null)
            {
                return;
            }

            foreach (RayButton button in Buttons)
            {
                button.SetPressed(false);
            }

            PressedCount = 0;
            counter.Show(PressedCount, TotalCount, "CLEARED");
        }
    }
}
