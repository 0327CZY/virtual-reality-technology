namespace QuestCounter
{
    /// <summary>RESET ALL 按钮：被射线选中并触发时，清空全部按键。</summary>
    public sealed class ResetPanelTarget : RayTarget
    {
        public CurvedButtonPanel Panel;

        public override void Activate()
        {
            if (Panel != null)
            {
                Panel.ResetAll();
            }
        }
    }
}
