using System.Text;
using Unity.Entities;
using UnityEngine;
namespace BAStudio.ECSSprite
{
    public class PooledBufferIndexInspector : MonoBehaviour
    {
        public World world { get; set; }
        string latestCached;
        StringBuilder stringBuilder = new StringBuilder(256);
        int updateInterval = 60;
        int tick = 60;

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
            if (world == null) return;
            var srs = world.GetExistingSystem<SpriteRenderingSystem>();
            for (int i = 0; i < srs.Baked; i++)
            {
                stringBuilder.AppendFormat("\nSheet {0} pooled: ", i);
                for (int j = 0; j < srs.freeIndexs[i].Length; j++)
                {
                    stringBuilder.AppendFormat("{0},", srs.freeIndexs[i][j]);
                }
            }
            latestCached = stringBuilder.ToString();
            stringBuilder.Clear();            
        }

        #if UNITY_EDITOR
        void OnGUI ()
        {
            if (latestCached == null) return;
            GUILayout.Space(128);
            GUILayout.Label(latestCached);
        }
        #endif
    }
}