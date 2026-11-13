using UnityEngine;

namespace QuestCounter
{
    /// <summary>
    /// 场景入口。Main.unity 里只有这一个组件，相机、按键面板、计数板、
    /// 复位键和射线指针都在 Awake 里用代码生成。
    /// </summary>
    public sealed class DemoBootstrap : MonoBehaviour
    {
        private static readonly Color LightBlue = new Color(0.6f, 0.8f, 0.95f);

        private PanelMaterials materials;
        private XRTrackedRig rig;
        private CurvedButtonPanel panel;
        private TextMesh statusText;
        private string shownStatus;

        private void Awake()
        {
            Application.targetFrameRate = 72;   // Quest 2 的常用刷新率
            materials = new PanelMaterials();

            Camera camera = CreateCamera();
            Transform hand = new GameObject("Tracked right hand ray origin").transform;
            hand.SetParent(transform, false);
            rig = gameObject.AddComponent<XRTrackedRig>();
            rig.Initialize(camera, hand);

            CounterBoard board = CreateCounterBoard();
            panel = CreatePanel(board);
            CreateTitles();
            CreateGround();
            CreateResetButton();
            statusText = ScenePrimitives.Label("Tracking status", transform, new Vector3(0f, 0.84f, 1.98f), "", 0.03f, LightBlue);

            GameObject pointer = new GameObject("Ray pointer");
            pointer.transform.SetParent(transform, false);
            pointer.AddComponent<RayPointer>().Initialize(rig, materials);
        }

        private Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 30f;
            camera.fieldOfView = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        // 计数板放在面板右侧，向内转 34 度，朝向站在原点的用户。
        private CounterBoard CreateCounterBoard()
        {
            GameObject boardObject = new GameObject("Counter beside panel");
            boardObject.transform.SetParent(transform, false);
            boardObject.transform.localPosition = new Vector3(1.25f, 1.67f, 1.85f);
            boardObject.transform.localRotation = Quaternion.Euler(0f, 34f, 0f);

            CounterBoard board = boardObject.AddComponent<CounterBoard>();
            board.Build(materials);
            return board;
        }

        private CurvedButtonPanel CreatePanel(CounterBoard board)
        {
            GameObject panelObject = new GameObject("Curved button panel");
            panelObject.transform.SetParent(transform, false);

            CurvedButtonPanel result = panelObject.AddComponent<CurvedButtonPanel>();
            result.Build(materials, board);
            return result;
        }

        private void CreateTitles()
        {
            ScenePrimitives.Label("Title", transform, new Vector3(0f, 2.36f, 1.96f), "RAYCAST / CURVED COUNTER", 0.065f, Color.white);
            ScenePrimitives.Label("Grid size", transform, new Vector3(0f, 2.26f, 1.96f), "30 COLUMNS   x   20 ROWS   /   600 BUTTONS", 0.032f, LightBlue);
        }

        private void CreateGround()
        {
            ScenePrimitives.Box("Ground", transform, new Vector3(0f, -0.04f, 1f), new Vector3(10f, 0.05f, 10f), materials.Floor, true);
        }

        // 面板下方的 RESET ALL：同样用射线选中，触发后清空所有按键。
        private void CreateResetButton()
        {
            Vector3 size = new Vector3(0.47f, 0.13f, 0.04f);

            GameObject reset = new GameObject("Reset all buttons");
            reset.transform.SetParent(transform, false);
            reset.transform.localPosition = new Vector3(0f, 0.98f, 2f);
            reset.AddComponent<BoxCollider>().size = size;
            reset.AddComponent<ResetPanelTarget>().Panel = panel;

            ScenePrimitives.Box("Reset face", reset.transform, Vector3.zero, size, materials.Reset);
            ScenePrimitives.Label("Reset label", reset.transform, new Vector3(0f, 0f, -0.025f), "RESET ALL", 0.06f, Color.white);
        }

        private void Update()
        {
            // 桌面预览：按 R 清空。
            if (!rig.IsXRActive && Input.GetKeyDown(KeyCode.R))
            {
                panel.ResetAll();
            }

            ShowTrackingStatus();
        }

        private void ShowTrackingStatus()
        {
            string status;
            if (!rig.IsXRActive)
            {
                status = "DESKTOP / MOUSE";
            }
            else if (!rig.HeadTracked)
            {
                status = "HEADSET NOT TRACKED";
            }
            else if (!rig.HandTracked)
            {
                status = "RIGHT HAND NOT TRACKED";
            }
            else
            {
                status = "RIGHT HAND / TRACKED";
            }

            // 文字没变就不重写，避免每帧重建网格。
            if (status != shownStatus)
            {
                statusText.text = status;
                shownStatus = status;
            }
        }

        private void OnGUI()
        {
            if (rig == null || rig.IsXRActive)
            {
                return;
            }

            GUI.Box(new Rect(12, 12, 440, 64),
                "DESKTOP PREVIEW\nLeft click: toggle | Right drag: look | WASD: move | R: clear\nQuest 2: aim with the RIGHT controller and squeeze the trigger.");
        }

        private void OnDestroy()
        {
            if (materials != null)
            {
                materials.Dispose();
            }
        }
    }
}
