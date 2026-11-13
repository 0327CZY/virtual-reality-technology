namespace QuestCounter
{
    /// <summary>
    /// 把"扳机/鼠标左键"的每帧状态变成"刚刚按下"这一个事件。
    /// 规则：
    ///   - 只有"上一帧没按、这一帧按下"才算一次触发，按住不放只触发一次；
    ///   - 输入不可用（追踪丢失、窗口失焦）时重新武装，恢复时若仍按着不误触；
    ///   - 输入源在手柄和鼠标之间切换时也重新武装，切换瞬间已按住的输入不算新点击。
    /// 这里只处理布尔状态，不依赖 Unity，方便单独测试。
    /// </summary>
    public sealed class TriggerEdge
    {
        // 初始视为"按着"，这样开局就按着扳机时，必须先松开才会触发。
        private bool wasPressed = true;
        private bool hasSource;
        private bool lastSourceIsXR;

        /// <summary>
        /// 传入这一帧的状态，返回是否构成一次新的触发。
        /// pressed：扳机或左键是否按下；available：输入是否可用；sourceIsXR：当前输入来自手柄还是鼠标。
        /// </summary>
        public bool Evaluate(bool pressed, bool available, bool sourceIsXR)
        {
            // 只有"从已知的上一个输入源真正切换"才算切换。第一次调用不算：
            // 此时 wasPressed 初始为 true，已经能挡住开局就按着的情况。
            bool switched = hasSource && sourceIsXR != lastSourceIsXR;
            hasSource = true;
            lastSourceIsXR = sourceIsXR;

            if (!available || switched)
            {
                // 重新武装：忽略这一帧的按下，要求先松开再按。
                wasPressed = true;
                return false;
            }

            bool justPressed = pressed && !wasPressed;
            wasPressed = pressed;
            return justPressed;
        }
    }
}
