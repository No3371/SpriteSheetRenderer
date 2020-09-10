using System.Text;
using Unity.Entities;
using UnityEngine;
namespace BAStudio.ECSSprite
{
    public class UVIndexBufferGUI : MonoBehaviour
    {
        public World world { get; set; }
        string latestCached;
        int updateInterval = 60;
        int tick = 60;
        StringBuilder stringBuilder = new StringBuilder(256);

        void Update ()
        {
            tick--;
            if (tick > 0) return;
            tick = updateInterval;
            if (world == null) return;
            Get();
        }

        void Get ()
        {
            var srs = world.GetExistingSystem<SpriteRenderingSystem>();
            for (int i = 0; i < srs.Baked; i++)
            {
                stringBuilder.AppendFormat("\nSheet {0} uv pointers: ", i);
                for (int j = 0; j < srs.bakedSpriteSheets[i].Users; j++)
                {
                    stringBuilder.AppendFormat("{0},", srs.sheetInstanceUVPointers[i][j]);
                }
            }
            latestCached = stringBuilder.ToString();
            stringBuilder.Clear();
        }

        #if UNITY_EDITOR
        void OnGUI ()
        {
            if (latestCached == null) return;
            GUILayout.Label(latestCached);
        }
        #endif
    }
}