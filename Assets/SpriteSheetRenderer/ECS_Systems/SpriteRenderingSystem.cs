using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;

namespace BAStudio.ECSSprite
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class SpriteRenderingSystem : SystemBase
    {
        public const int MAX_BAKED = 256, DEFAULT_SHEET_CAPACITY = 32, DEFAULT_INSTANCE_CAPACITY = 256;
        public int Baked { get; private set; }
        public UnityEngine.Material BaseMateirial;
        public BakedSpriteSheet[] bakedSpriteSheets;
        public NativeArray<float4>[] spriteSheetUVs;
        public NativeArray<float4>[] sheetInstanceMatrixs;
        public NativeArray<float4>[] sheetInstanceColors;
        public NativeArray<int>[] sheetInstanceUVPointers;
        public NativeList<int>[] freeIndexs;
        public NativeArray<SpriteSheetPointer> filters;
        public Dictionary<string, int> BakedSpriteSheetMap;
        public NativeList<bool> BufferStructureChanged;
        public int Bake(string ID, UnityEngine.Sprite[] spriteSheet, int instanceCapacityOverride = DEFAULT_INSTANCE_CAPACITY)
        {
            UnityEngine.Texture textureCache = spriteSheet[0].texture;
            for (int i = 0; i < spriteSheet.Length; i++) if (spriteSheet[i].texture != textureCache) throw new System.ArgumentException("Baking sprites not within same texture is not allowed!");

            float w = textureCache.width;
            float h = textureCache.height;
            float4[] uvs = new float4[spriteSheet.Length];
            for (int i = 0; i < spriteSheet.Length; i++)
            {
                float tilingX = 1f / (w / spriteSheet[i].rect.width);
                float tilingY = 1f / (h / spriteSheet[i].rect.height);
                float OffsetX = tilingX * (spriteSheet[i].rect.x / spriteSheet[i].rect.width);
                float OffsetY = tilingY * (spriteSheet[i].rect.y / spriteSheet[i].rect.height);
                uvs[i].x = tilingX;
                uvs[i].y = tilingY;
                uvs[i].z = OffsetX;
                uvs[i].w = OffsetY;
            }

            UnityEngine.Material newMat = UnityEngine.Material.Instantiate(BaseMateirial);
            newMat.mainTexture = textureCache;
            BakedSpriteSheet newBaked = new BakedSpriteSheet(Baked, spriteSheet.Length, newMat);
            bakedSpriteSheets[Baked] = newBaked;

            SpriteSheetPointer newFilter = new SpriteSheetPointer { spriteSheetIndex = Baked };
            filters[Baked] = newFilter;

            BufferStructureChanged.Add(true);

            spriteSheetUVs[Baked] = new NativeArray<float4>(uvs, Allocator.Persistent);
            newBaked.UpdateUVBuffers(spriteSheetUVs[Baked]);
            newBaked.Capacity = (uint)instanceCapacityOverride;
            sheetInstanceUVPointers[Baked] = new NativeArray<int>(instanceCapacityOverride, Allocator.Persistent);
            sheetInstanceColors[Baked] = new NativeArray<float4>(instanceCapacityOverride, Allocator.Persistent);
            sheetInstanceMatrixs[Baked] = new NativeArray<float4>(instanceCapacityOverride, Allocator.Persistent);
            freeIndexs[Baked] = new NativeList<int>(8, Allocator.Persistent);
            BakedSpriteSheetMap[ID] = Baked;
            Baked++;
            if (Baked >= bakedSpriteSheets.Length) GrowBuffersForNewSpriteSheet();
            UnityEngine.Debug.Log(string.Format("Baked sprite sheet: {0}! Current baked: {1}", ID, Baked));
            return Baked;
        }

        void GrowBuffersForNewSpriteSheet()
        {
            if (bakedSpriteSheets.Length == MAX_BAKED) throw new System.OverflowException("Exceeding MAX_BAKED setting.");
            BakedSpriteSheet[] old = bakedSpriteSheets;
            bakedSpriteSheets = new BakedSpriteSheet[old.Length * 2];
            old.CopyTo(bakedSpriteSheets, 0);
            old = null;

            NativeArray<SpriteSheetPointer> oldFilters = filters;
            filters = new NativeArray<SpriteSheetPointer>(bakedSpriteSheets.Length, Allocator.Persistent);
            oldFilters.Slice().CopyToFast(filters);
            oldFilters.Dispose();

            NativeArray<float4>[] oldUVs = spriteSheetUVs;
            spriteSheetUVs = new NativeArray<float4>[bakedSpriteSheets.Length];
            oldUVs.CopyTo(spriteSheetUVs, 0);
            oldUVs = null;

            NativeArray<float4>[] oldMatrixs = sheetInstanceMatrixs;
            sheetInstanceMatrixs = new NativeArray<float4>[bakedSpriteSheets.Length];
            oldMatrixs.CopyTo(sheetInstanceMatrixs, 0);
            oldMatrixs = null;

            NativeArray<int>[] oldUVPointers = sheetInstanceUVPointers;
            sheetInstanceUVPointers = new NativeArray<int>[bakedSpriteSheets.Length];
            oldUVPointers.CopyTo(sheetInstanceUVPointers, 0);
            oldUVPointers = null;

            NativeArray<float4>[] oldColors = sheetInstanceColors;
            sheetInstanceColors = new NativeArray<float4>[bakedSpriteSheets.Length];
            oldColors.CopyTo(sheetInstanceColors, 0);
            oldColors = null;
        }

        internal void GrowBuffersForNewUser(int sheetIndex, int capOveride = -1)
        {
            int newCap = capOveride == -1 ? (int)bakedSpriteSheets[sheetIndex].Capacity * 2 : capOveride;
            bakedSpriteSheets[sheetIndex].Capacity = (uint)newCap;
            NativeArray<float4> oldMatrixs = sheetInstanceMatrixs[sheetIndex];
            sheetInstanceMatrixs[sheetIndex] = new NativeArray<float4>(newCap, Allocator.Persistent);
            oldMatrixs.Slice().CopyToFast(sheetInstanceMatrixs[sheetIndex]);
            oldMatrixs.Dispose();

            NativeArray<int> oldUVPointers = sheetInstanceUVPointers[sheetIndex];
            sheetInstanceUVPointers[sheetIndex] = new NativeArray<int>(newCap, Allocator.Persistent);
            oldUVPointers.Slice().CopyToFast(sheetInstanceUVPointers[sheetIndex]);
            oldUVPointers.Dispose();

            NativeArray<float4> oldColors = sheetInstanceColors[sheetIndex];
            sheetInstanceColors[sheetIndex] = new NativeArray<float4>(newCap, Allocator.Persistent);
            oldColors.Slice().CopyToFast(sheetInstanceColors[sheetIndex]);
            oldColors.Dispose();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < bakedSpriteSheets.Length; i++)
            {
                if (sheetInstanceMatrixs[i].IsCreated)    sheetInstanceMatrixs[i].Dispose();
                if (sheetInstanceColors[i].IsCreated)     sheetInstanceColors[i].Dispose();
                if (spriteSheetUVs[i].IsCreated)          spriteSheetUVs[i].Dispose();
                if (sheetInstanceUVPointers[i].IsCreated) sheetInstanceUVPointers[i].Dispose();
                if (freeIndexs[i].IsCreated)              freeIndexs[i].Dispose();
            }
            BufferStructureChanged.Dispose();
            filters.Dispose();
        }
        // public SpriteRenderSubject NewRenderSubeject (int sheetIndex, int uvIndex)
        // {
        //     int index;
        //     if (freeIndexs[sheetIndex].Length > 0)
        //     {
        //         index = freeIndexs[sheetIndex].ElementAt(freeIndexs[sheetIndex].Length - 1);
        //         freeIndexs[sheetIndex].Length -= 1; 
        //     }
        //     else
        //     {
        //         index = (int) bakedSpriteSheets[sheetIndex].Users;
        //         bakedSpriteSheets[sheetIndex].Users += 1;
        //     }

        //     return new SpriteRenderSubject
        //     {
        //         bufferIndex = index,
        //         uvIndex = uvIndex
        //     };
        // }
        public JobHandle MatrixUpdateHandle;
        public bool MatrixUpdateThisFrame { get; set; }
        List<EntityQuery> queries;
        private UnityEngine.Mesh mesh;
        EntityQuery removedSubjectQuery;
        protected override void OnCreate()
        {
            removedSubjectQuery = GetEntityQuery(ComponentType.ReadOnly<SpriteRenderSubject>(),
                                                 ComponentType.ReadOnly<SpriteSheetPointer>(),
                                                 ComponentType.Exclude<SpriteMatrix>());
            spriteSheetUVs = new NativeArray<float4>[DEFAULT_SHEET_CAPACITY];
            bakedSpriteSheets = new BakedSpriteSheet[DEFAULT_SHEET_CAPACITY];
            sheetInstanceMatrixs = new NativeArray<float4>[DEFAULT_SHEET_CAPACITY];
            sheetInstanceColors = new NativeArray<float4>[DEFAULT_SHEET_CAPACITY];
            sheetInstanceUVPointers = new NativeArray<int>[DEFAULT_SHEET_CAPACITY];
            freeIndexs = new NativeList<int>[DEFAULT_SHEET_CAPACITY];
            filters = new NativeArray<SpriteSheetPointer>(DEFAULT_SHEET_CAPACITY, Allocator.Persistent);
            BakedSpriteSheetMap = new Dictionary<string, int>(DEFAULT_SHEET_CAPACITY);
            mesh = MeshExtension.Quad();
            BufferStructureChanged = new NativeList<bool>(DEFAULT_SHEET_CAPACITY, Allocator.Persistent);
        }

        UnityEngine.Bounds DrawBounds { get; set; } = new UnityEngine.Bounds(UnityEngine.Vector2.zero, UnityEngine.Vector3.one * 100);
        public System.Func<UnityEngine.Bounds> BoundsUpdator { get; set; }
        protected override void OnUpdate()
        {
            if (removedSubjectQuery.CalculateEntityCount() > 0)
            {
                var removed = removedSubjectQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < removed.Length; i++)
                {
                    SpriteRenderSubject removedSubject = EntityManager.GetComponentData<SpriteRenderSubject>(removed[i]);
                    SpriteSheetPointer removedSubjectPointer = EntityManager.GetSharedComponentData<SpriteSheetPointer>(removed[i]);
                    int sheet = removedSubjectPointer.spriteSheetIndex;
                    int bufferIndex = removedSubject.bufferIndex;
                    // UnityEngine.Debug.Log(string.Format("Removing {0}, which points to {1} and using buffer {2}", removed[i].Index, sheet, bufferIndex));
                    freeIndexs[sheet].Add(bufferIndex);
                    // UnityEngine.Debug.Log(string.Format("Sheet {0} pooled: {1} ", sheet, freeIndexs[sheet].Length));
                    sheetInstanceUVPointers[sheet][bufferIndex] = -1;
                    BufferStructureChanged[sheet] = true;
                }
                EntityManager.RemoveComponent(removed, typeof(SpriteRenderSubject));
                EntityManager.RemoveComponent(removed, typeof(SpriteSheetPointer));
                EntityManager.RemoveComponent(removed, typeof(BufferedRenderSubjectTag));
                removed.Dispose();
            }

            if (BoundsUpdator != null) DrawBounds = BoundsUpdator();

            for (int i = 0; i < Baked; i++)
            {
                if (bakedSpriteSheets[i].Users > 0)
                {
                    bakedSpriteSheets[i].UpdateInstanceDataBuffers((int)bakedSpriteSheets[i].Users, sheetInstanceColors[i], sheetInstanceMatrixs[i], sheetInstanceUVPointers[i]);
                    bakedSpriteSheets[i].Material.SetVector("_RenderBounds", new UnityEngine.Vector4(DrawBounds.center.x, DrawBounds.center.y, 0, 0));
                    UnityEngine.Graphics.DrawMeshInstancedIndirect(mesh,
                                                                   0,
                                                                   bakedSpriteSheets[i].Material,
                                                                   new UnityEngine.Bounds(DrawBounds.center, UnityEngine.Vector3.one * 100),
                                                                   bakedSpriteSheets[i].ArgsBuffer);
                }
            }
        }
    }
}