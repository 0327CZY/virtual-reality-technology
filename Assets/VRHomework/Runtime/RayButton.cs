using UnityEngine;

namespace QuestCounter
{
    /// <summary>
    /// 一个按键。它自己记住"松开/按下"，并负责外观：按下时变红并向后凹陷。
    /// </summary>
    public sealed class RayButton : RayTarget
    {
        // 按下时键帽沿按键自身的 +Z 方向后移的距离（米）。
        // 按键朝向由曲面生成时决定，+Z 正好是圆柱的外法线，也就是远离用户的方向。
        private const float PressDepth = 0.025f;

        private CurvedButtonPanel panel;
        private PanelMaterials materials;
        private Transform cap;
        private Renderer capRenderer;

        public int Row { get; private set; }
        public int Column { get; private set; }
        public bool IsPressed { get; private set; }
        public Transform Cap { get { return cap; } }

        public void Setup(CurvedButtonPanel owner, int row, int column, Transform capTransform, PanelMaterials palette)
        {
            panel = owner;
            Row = row;
            Column = column;
            cap = capTransform;
            capRenderer = cap.GetComponent<Renderer>();
            materials = palette;
            SetPressed(false);
        }

        // 射线选中并扣下扳机时调用：切换状态，再让面板更新计数。
        public override void Activate()
        {
            SetPressed(!IsPressed);
            panel.OnButtonChanged(this);
        }

        // 只改变状态和外观，不通知面板。"全部清空"直接用它。
        public void SetPressed(bool pressed)
        {
            IsPressed = pressed;
            cap.localPosition = pressed ? Vector3.forward * PressDepth : Vector3.zero;
            capRenderer.sharedMaterial = pressed ? materials.Pressed : materials.Released;
        }
    }
}
