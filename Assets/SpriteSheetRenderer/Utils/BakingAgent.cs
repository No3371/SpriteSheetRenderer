using System.Collections;
using System.Collections.Generic;
using BAStudio.ECSSprite;
using Unity.Entities;
using UnityEngine;

public class BakingAgent : MonoBehaviour
{
    public List<BakingPreset> presets;
    public void Bake (World world)
    {
        if (presets == null || presets.Count == 0) return;
        SpriteRenderingSystem srs = world.GetExistingSystem<SpriteRenderingSystem>();
        for (int p = 0; p < presets.Count; p++)
            if (presets[p].useInitInstanceCap) srs.Bake(presets[p].ID, presets[p].sprites, presets[p].initInstancesCap);
            else srs.Bake(presets[p].ID, presets[p].sprites);
    }
}
