using System.Collections;
using System.Collections.Generic;
using BAStudio.ECSSprite;
using Unity.Entities;
using UnityEngine;

public class SpriteRenderingSystemInitializer : MonoBehaviour
{
    public Material instancedMaterial;
    public List<BakingPreset> presets;
    public void Initialize (World world)
    {
        if (presets == null || presets.Count == 0) return;
        SpriteRenderingSystem srs = world.GetExistingSystem<SpriteRenderingSystem>();
        srs.BaseMateirial = instancedMaterial;
        for (int p = 0; p < presets.Count; p++)
            if (presets[p].useInitInstanceCap) srs.Bake(presets[p].ID, presets[p].sprites, presets[p].initInstancesCap);
            else srs.Bake(presets[p].ID, presets[p].sprites);
    }

    public void AddSystems (World world)
    {
        world.GetOrCreateSystem<SpriteRenderingSystem>();
        world.GetOrCreateSystem<SpriteRendererUpdateBufferSystem>();
        world.GetOrCreateSystem<SpriteRenderingBufferMaintenenceSystem>();
        world.GetOrCreateSystem<UpdateMatrixSystem>();
    }
}
