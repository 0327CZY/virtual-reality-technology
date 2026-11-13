using System;
using UnityEngine;

namespace QuestCounter
{
    /// <summary>
    /// 场景里用到的几种纯色材质。所有按键共用同一个"松开"或"按下"材质，
    /// 状态切换时只是换一下引用，不会为 600 个按键各创建一份材质。
    /// </summary>
    public sealed class PanelMaterials : IDisposable
    {
        public Material Released { get; private set; }
        public Material Pressed { get; private set; }
        public Material Shell { get; private set; }
        public Material Floor { get; private set; }
        public Material Ray { get; private set; }
        public Material Hit { get; private set; }
        public Material Reset { get; private set; }

        private readonly Material[] all;

        public PanelMaterials()
        {
            // 通过 Resources.Load 引用着色器，Android 打包时它才不会被裁剪掉。
            Shader shader = Resources.Load<Shader>("PanelColor");
            if (shader == null)
            {
                throw new InvalidOperationException("Missing Resources/PanelColor.shader");
            }

            Released = Make(shader, new Color(0.86f, 0.56f, 0.27f), "Released");   // 橙色
            Pressed = Make(shader, new Color(1f, 0.16f, 0.12f), "Pressed");        // 红色
            Shell = Make(shader, new Color(0.055f, 0.085f, 0.13f), "Housing");
            Floor = Make(shader, new Color(0.12f, 0.17f, 0.22f), "Floor");
            Ray = Make(shader, new Color(0.18f, 0.85f, 1f), "Ray");
            Hit = Make(shader, new Color(0.45f, 1f, 0.42f), "Hit");                // 射线指向可选目标时
            Reset = Make(shader, new Color(0.12f, 0.49f, 0.65f), "Reset");

            all = new[] { Released, Pressed, Shell, Floor, Ray, Hit, Reset };
        }

        private static Material Make(Shader shader, Color color, string name)
        {
            Material material = new Material(shader);
            material.color = color;
            material.name = name;
            material.enableInstancing = true;
            return material;
        }

        public void Dispose()
        {
            foreach (Material material in all)
            {
                if (material != null)
                {
                    ScenePrimitives.DestroyObject(material);
                }
            }
        }
    }
}
