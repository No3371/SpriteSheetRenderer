using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteBakingPreset", menuName = "Custom/SpriteBakingPreset")]
public class BakingPreset : ScriptableObject
{
    public string ID;
    public Sprite[] sprites;
    public int initInstancesCap;
    public bool useInitInstanceCap;
    void OnValidate ()
    {
        if (sprites != null) for (int i = 0; i < sprites.Length; i++) if (sprites[i].texture != sprites[0].texture) Debug.LogError("All sprites in 1 baking set must be from same texture asset!");
    }
}
