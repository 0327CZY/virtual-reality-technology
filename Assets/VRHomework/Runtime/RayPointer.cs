using UnityEngine;
using UnityEngine.Rendering;

namespace QuestCounter
{
    /// <summary>
    /// 光线投射指针：每帧从手柄（桌面预览时从鼠标）发出一条射线，
    /// 射线打到可选目标时变色提示，扳机刚扣下的那一帧触发目标。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RayPointer : MonoBehaviour
    {
        private const float Reach = 8f;

        private XRTrackedRig rig;
        private PanelMaterials materials;
        private LineRenderer line;
        private Transform cursor;
        private Renderer cursorRenderer;

        // 把每帧的扳机状态变成"刚刚按下"事件，并处理按住、失焦和输入源切换。
        private readonly TriggerEdge trigger = new TriggerEdge();

        public void Initialize(XRTrackedRig trackedRig, PanelMaterials palette)
        {
            rig = trackedRig;
            materials = palette;

            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.003f;
            line.endWidth = 0.002f;
            line.numCapVertices = 4;
            line.sharedMaterial = materials.Ray;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            // 射线终点的小圆球，指示命中位置。它不能有碰撞体，否则会挡住射线自己。
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Ray hit cursor";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 0.014f;
            ScenePrimitives.RemoveCollider(sphere);
            cursor = sphere.transform;
            cursorRenderer = sphere.GetComponent<Renderer>();
            cursorRenderer.sharedMaterial = materials.Ray;
        }

        private void Update()
        {
            if (rig == null)
            {
                return;
            }

            bool isXR = rig.IsXRActive;
            Ray ray;
            bool pressed;
            bool hasInput = isXR ? ReadController(out ray, out pressed) : ReadMouse(out ray, out pressed);

            // 触发判定交给 TriggerEdge：它负责按住不连点、失焦/追踪丢失、以及输入源切换的重新武装。
            bool justPressed = trigger.Evaluate(pressed, hasInput, isXR);

            if (!hasInput)
            {
                SetVisible(false);
                return;
            }

            RayTarget target;
            Vector3 end;
            bool canSelect = TryGetTarget(ray, Reach, out target, out end);

            SetVisible(true);
            DrawRay(ray, end, canSelect);

            if (justPressed && canSelect)
            {
                target.Activate();
                if (rig.IsXRActive)
                {
                    rig.Pulse();
                }
            }
        }

        // 手柄：射线从右手位置沿手柄正前方射出。
        private bool ReadController(out Ray ray, out bool pressed)
        {
            ray = new Ray(rig.Hand.position, rig.Hand.forward);
            pressed = false;
            return Application.isFocused && rig.TryReadTriggerPressed(out pressed);
        }

        // 桌面预览：射线从相机穿过鼠标位置射出。按住右键是转动视角，此时不选取。
        private bool ReadMouse(out Ray ray, out bool pressed)
        {
            ray = rig.View.ScreenPointToRay(Input.mousePosition);
            pressed = Input.GetMouseButton(0);
            return Application.isFocused && !Input.GetMouseButton(1);
        }

        private void DrawRay(Ray ray, Vector3 end, bool canSelect)
        {
            // 桌面预览时射线本身从鼠标位置出发，画出来的线改从相机右下方起笔，才看得见。
            Vector3 start = ray.origin;
            if (!rig.IsXRActive)
            {
                start = rig.View.transform.TransformPoint(new Vector3(0.18f, -0.18f, 0.25f));
            }

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            // 指向可选目标时射线和光标变绿。
            Material material = canSelect ? materials.Hit : materials.Ray;
            line.sharedMaterial = material;
            cursorRenderer.sharedMaterial = material;

            // 光标略微退回射线一侧，避免和被击中的表面重叠闪烁。
            cursor.position = end - ray.direction * 0.008f;
        }

        private void SetVisible(bool visible)
        {
            line.enabled = visible;
            cursor.gameObject.SetActive(visible);
        }

        // 取射线打到的第一个碰撞体。墙等遮挡物也会被打到，所以不能隔着它选中后面的按键。
        public static bool TryGetTarget(Ray ray, float reach, out RayTarget target, out Vector3 end)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                target = hit.collider.GetComponentInParent<RayTarget>();
                return target != null && target.isActiveAndEnabled;
            }

            target = null;
            end = ray.GetPoint(reach);
            return false;
        }
    }
}
