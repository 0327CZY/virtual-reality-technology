using UnityEngine;
using UnityEngine.Rendering;

namespace QuestCounter
{
    /// <summary>用代码创建立方体和文字的小工具。</summary>
    public static class ScenePrimitives
    {
        // withCollider 为 false 时，立方体只用来显示，射线会直接穿过。
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool withCollider = false)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;

            Renderer renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (!withCollider)
            {
                RemoveCollider(box);
            }
            return box;
        }

        public static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                DestroyObject(collider);
            }
        }

        // 播放时用 Destroy；编辑器测试里没有播放，要用 DestroyImmediate。
        public static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        public static TextMesh Label(string name, Transform parent, Vector3 localPosition, string text, float size, Color color)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.characterSize = size * 0.3f;
            label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.GetComponent<Renderer>().sharedMaterial = font.material;
            return label;
        }
    }
}
