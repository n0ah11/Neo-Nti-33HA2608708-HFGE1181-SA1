// SA1 Casual Top-Down - project builder (Neo Nti 33HA2608708)
// Menu: SA1 > Build Everything. Also runs automatically once if Assets/Editor/BUILD_NOW.txt exists.
// It prepares the provided assets (import settings, tileset slicing, tiles, palette), creates the
// animations, particle effects and prefabs, and builds the MainMenu and Game scenes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SA1Builder
{
    const string MARKER = "Assets/Editor/BUILD_NOW.txt";
    const string ART = "Assets/Art/";
    const string FONT = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const int MAP_W = 72, MAP_H = 48;

    static readonly string[] LEVEL = {
        "########################################################################",
        "########################################################################",
        "##....................................................................##",
        "##....................................................................##",
        "##.................................................################...##",
        "##...################......#############.####......#.......#......#...##",
        "##...#..............#......#................#......#.......#......#...##",
        "##...#..............#......#................#......#.......#......#...##",
        "##...#...##.........#..........##......##...#......#.......#......#...##",
        "##...#...##....................##......##...#..............#......#...##",
        "##...#..............#......#................#.....................#...##",
        "##...#.........##...#......#................#.....................#...##",
        "##...#.........##...#......#................#......#.......#......#...##",
        "##...#..............#......#######....#######......#.......#......#...##",
        "##...#..............#..............................#.......#......#...##",
        "##...######...#######..............................#.......#......#...##",
        "##.................................................#.......#......#...##",
        "##.................................................#.......#......#...##",
        "##......................##....................##...####......######...##",
        "##......................##....................##......................##",
        "##..........................########.#######..........................##",
        "##..........................#..............#..........................##",
        "##..........................#..............#..........................##",
        "##..........##.............................#..........................##",
        "##..........##...................######...............................##",
        "##...............................######...................##..........##",
        "##.........................................#..............##..........##",
        "##.........................................#..........................##",
        "##..........................#..............#..........................##",
        "##..........................#..............#..........................##",
        "##..........................######...#######..........................##",
        "##...#####....#########...............................................##",
        "##...#................#..........................#########..#######...##",
        "##...#................#..........................#................#...##",
        "##...#................#..........................#................#...##",
        "##...#...##.....##....#...........................................#...##",
        "##...#...##.....##...................................#...#...#....#...##",
        "##...#...##.......................................................#...##",
        "##...#...##...........#...........................................#...##",
        "##...#...##...........#............##.............................#...##",
        "##...#................#....##......##........##..#...#...#...#....#...##",
        "##...#................#....##................##..#................#...##",
        "##...#................#..........................#................#...##",
        "##...##################..........................##################...##",
        "##....................................................................##",
        "##....................................................................##",
        "########################################################################",
        "########################################################################"

    };

    // colours taken from the donut art
    static readonly Color Pink = new Color(1f, 0.49f, 0.85f);
    static readonly Color Cream = new Color(1f, 0.85f, 0.62f);
    static readonly Color Brown = new Color(0.36f, 0.17f, 0.05f);
    static readonly Color DarkBrown = new Color(0.2f, 0.09f, 0.02f);

    static TMP_FontAsset font;
    static int L_WALLS, L_UNITS, L_BODIES;

    static SA1Builder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MARKER)) return;
            File.Delete(MARKER);
            if (File.Exists(MARKER + ".meta")) File.Delete(MARKER + ".meta");
            AssetDatabase.Refresh();
            BuildAll();
        };
    }

    [MenuItem("SA1/Build Everything")]
    public static void BuildAll()
    {
        try
        {
            L_WALLS = LayerMask.NameToLayer("Walls");
            L_UNITS = LayerMask.NameToLayer("Units");
            L_BODIES = LayerMask.NameToLayer("Bodies");
            if (L_WALLS < 0 || L_UNITS < 0 || L_BODIES < 0) { Debug.LogError("SA1: layers Walls/Units/Bodies missing"); return; }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
            EnsureSortingLayers("Floor", "Walls", "Units", "FX");

            EnsureFolders();
            Step("Import settings", ConfigureImports);
            Step("Tiles", BuildTiles);
            Step("Particles", BuildParticles);
            Step("Animations", BuildAnimations);
            Step("Prefabs", BuildPrefabs);
            Step("Text material", MakeOutlineMaterial);
            Step("Game scene", BuildGameScene);
            Step("Main menu scene", BuildMenuScene);
            Step("Project settings", ConfigureProject);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("SA1: BUILD COMPLETE");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    static void Step(string name, Action a)
    {
        EditorUtility.DisplayProgressBar("SA1 Builder", name, 0.5f);
        try { a(); Debug.Log("SA1: " + name + " OK"); }
        catch (Exception e) { Debug.LogError("SA1: " + name + " FAILED\n" + e); throw; }
    }

    // adds any missing sorting layers (in order) to the TagManager
    static void EnsureSortingLayers(params string[] names)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("m_SortingLayers");
        foreach (var n in names)
        {
            bool found = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) { found = true; break; }
            if (found) continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var el = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            el.FindPropertyRelative("name").stringValue = n;
            el.FindPropertyRelative("uniqueID").longValue = (uint)(n.GetHashCode() & 0x7fffffff);
        }
        tm.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolders()
    {
        foreach (var f in new[] { "Assets/Animations", "Assets/Animations/Player", "Assets/Animations/Enemy", "Assets/Animations/Menu",
                     "Assets/Prefabs", "Assets/Prefabs/Characters", "Assets/Prefabs/Pickups", "Assets/Prefabs/Particles", "Assets/Prefabs/UI",
                     "Assets/Materials", "Assets/Scenes", "Assets/Tiles", "Assets/Tiles/Palette", "Assets/Physics" })
        {
            if (AssetDatabase.IsValidFolder(f)) continue;
            string parent = Path.GetDirectoryName(f).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
        }
    }

    // ------------------------------------------------------------------ helpers
    static void Set(Object o, string prop, object v)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogError("SA1: property '" + prop + "' not found on " + o.GetType().Name); return; }
        switch (v)
        {
            case float f: p.floatValue = f; break;
            case int i: p.intValue = i; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector2 v2: p.vector2Value = v2; break;
            case Vector3 v3: p.vector3Value = v3; break;
            case Rect r: p.rectValue = r; break;
            case Color c: p.colorValue = c; break;
            case LayerMask lm: p.intValue = lm.value; break;
            case Object ob: p.objectReferenceValue = ob; break;
            case null: p.objectReferenceValue = null; break;
            default: Debug.LogError("SA1: unsupported type for " + prop); break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(Object o, string prop, Object[] values)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Sprite Spr(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    static Sprite PlayerFrame(int n) => Spr(ART + "Characters/Player/Player" + n.ToString("0000") + ".png");
    static Sprite EnemyFrame(int n) => Spr(ART + "Characters/Enemy/Enemy" + n.ToString("0000") + ".png");
    static Sprite[] CrumbSprites() => new[] { 0, 2, 4, 6, 8 }.Select(n => Spr(ART + "Food/Crumbs" + n.ToString("0000") + ".png")).ToArray();

    static GameObject Child(GameObject parent, string name, Vector3 localPos = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        return go;
    }

    static T LoadOrCreate<T>(string path, Func<T> create) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = create();
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    // ------------------------------------------------------------------ 1. import settings (asset preparation)
    static void ConfigureImports()
    {
        // All character / food art is 512x512 -> 512 pixels per unit = 1 world unit at size 1.
        // The level sheet uses 128px tiles -> 128 PPU = 1 world unit per tile. Same world scale for everything.
        foreach (var dir in new[] { ART + "Characters/Player", ART + "Characters/Enemy", ART + "Food" })
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
                ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), 512, false);
        ConfigureSprite(ART + "Tiles/Donut Level Assets.png", 128, true);
        SliceTileSheet(ART + "Tiles/Donut Level Assets.png");
    }

    static void ConfigureSprite(string path, float ppu, bool multiple)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 1024;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteExtrude = 1;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
    }

    // 8x8 grid of 128px cells, named Tile_r<row>_c<col> (row 0 = top of the image)
    static void SliceTileSheet(string path)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var existing = dp.GetSpriteRects().ToDictionary(r => r.name, r => r);
        var rects = new List<SpriteRect>();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                string name = "Tile_r" + r + "_c" + c;
                var sr = existing.ContainsKey(name) ? existing[name] : new SpriteRect { name = name, spriteID = GUID.Generate() };
                sr.rect = new Rect(c * 128, 1024 - (r + 1) * 128, 128, 128);
                sr.alignment = SpriteAlignment.Center;
                sr.pivot = new Vector2(0.5f, 0.5f);
                sr.border = Vector4.zero;
                rects.Add(sr);
            }
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
        ti.SaveAndReimport();
    }

    static Sprite TileSprite(int r, int c) =>
        AssetDatabase.LoadAllAssetsAtPath(ART + "Tiles/Donut Level Assets.png").OfType<Sprite>().First(s => s.name == "Tile_r" + r + "_c" + c);

    // ------------------------------------------------------------------ 2. tiles + palette
    // Floor variants keyed by which sides touch a wall (T,B,L,R) -> the cell in the sheet that has that outline.
    static readonly Dictionary<string, Vector2Int> FloorCells = new Dictionary<string, Vector2Int>
    {
        { "", new Vector2Int(2, 4) }, { "TLR", new Vector2Int(0, 4) }, { "TBL", new Vector2Int(1, 1) }, { "TR", new Vector2Int(1, 2) },
        { "LR", new Vector2Int(1, 4) }, { "TL", new Vector2Int(1, 6) }, { "TBR", new Vector2Int(1, 7) }, { "BL", new Vector2Int(2, 2) },
        { "TB", new Vector2Int(2, 3) }, { "BR", new Vector2Int(2, 6) }, { "B", new Vector2Int(4, 4) }, { "R", new Vector2Int(5, 2) },
        { "L", new Vector2Int(5, 6) }, { "BLR", new Vector2Int(6, 2) }, { "TBLR", new Vector2Int(6, 4) },
    };
    static Tile wallTile;
    static readonly Dictionary<string, Tile> floorTiles = new Dictionary<string, Tile>();

    static Tile MakeTile(string name, Sprite s, Tile.ColliderType col)
    {
        string path = "Assets/Tiles/" + name + ".asset";
        var t = LoadOrCreate(path, () => ScriptableObject.CreateInstance<Tile>());
        t.sprite = s;
        t.colliderType = col;
        t.color = Color.white;
        t.flags = TileFlags.None; // allows the per-cell flip used for the "top edge" variant
        EditorUtility.SetDirty(t);
        return t;
    }

    static void BuildTiles()
    {
        wallTile = MakeTile("Wall", TileSprite(0, 0), Tile.ColliderType.Grid);
        floorTiles.Clear();
        foreach (var kv in FloorCells)
            floorTiles[kv.Key] = MakeTile("Floor_" + (kv.Key == "" ? "Open" : kv.Key), TileSprite(kv.Value.x, kv.Value.y), Tile.ColliderType.None);
        AssetDatabase.SaveAssets();
        BuildPalette();
    }

    // Tile Palette so the level can be edited by hand with the Tile Palette window
    static void BuildPalette()
    {
        const string path = "Assets/Tiles/Palette/DonutPalette.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
        var go = new GameObject("DonutPalette");
        go.AddComponent<Grid>();
        var layer = Child(go, "Layer1");
        var tm = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        tm.SetTile(new Vector3Int(0, 0, 0), wallTile);
        int i = 1;
        foreach (var kv in floorTiles) { tm.SetTile(new Vector3Int(i % 6, -(i / 6), 0), kv.Value); i++; }
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        var gpType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.GridPalette")).FirstOrDefault(t => t != null);
        if (gpType != null)
        {
            var gp = ScriptableObject.CreateInstance(gpType);
            gp.name = "Palette Settings";
            AssetDatabase.AddObjectToAsset(gp, path);
            AssetDatabase.ImportAsset(path);
        }
    }

    static bool IsWall(int x, int row) => x < 0 || row < 0 || x >= MAP_W || row >= MAP_H || LEVEL[row][x] == '#';

    // paints the ASCII layout into the Floor and Walls tilemaps (row 0 = top). Pads with wall outside the map.
    static void PaintLevel(Tilemap floor, Tilemap walls, int pad)
    {
        floor.ClearAllTiles(); walls.ClearAllTiles();
        for (int row = -pad; row < MAP_H + pad; row++)
            for (int x = -pad; x < MAP_W + pad; x++)
            {
                var cell = new Vector3Int(x, MAP_H - 1 - row, 0);
                if (IsWall(x, row)) { walls.SetTile(cell, wallTile); continue; }
                string k = (IsWall(x, row - 1) ? "T" : "") + (IsWall(x, row + 1) ? "B" : "") + (IsWall(x - 1, row) ? "L" : "") + (IsWall(x + 1, row) ? "R" : "");
                if (k == "T")
                {
                    // no "top only" cell in the sheet -> use the "bottom only" cell flipped vertically
                    floor.SetTile(cell, floorTiles["B"]);
                    floor.SetTransformMatrix(cell, Matrix4x4.Scale(new Vector3(1, -1, 1)));
                }
                else floor.SetTile(cell, floorTiles[k]);
            }
    }

    // ------------------------------------------------------------------ 3. particles (Unity Particle System)
    static GameObject crumbBurst, growBurst, hitBurst, deathBurst;

    static Material SpriteMaterial(string name, Texture2D tex)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        var m = LoadOrCreate(path, () => new Material(shader));
        m.shader = shader;
        m.mainTexture = tex;
        EditorUtility.SetDirty(m);
        return m;
    }

    static ParticleSystem AddBurst(GameObject go, Material mat, int count, Vector2 speed, Vector2 size, Vector2 life,
                                   Color a, Color b, float radius, float gravity, bool ring = false)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; // bursts scale with the donut that spawned them
        main.maxParticles = 200;
        main.stopAction = ParticleSystemStopAction.None;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius;
        sh.radiusThickness = ring ? 0f : 1f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.7f, 0.8f), new Keyframe(1, 0)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
        col.color = g;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.dampen = 0.15f;
        limit.limit = 0.5f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingLayerName = "FX";
        r.sortingOrder = 10;
        return ps;
    }

    static GameObject SaveParticle(GameObject go, string name)
    {
        string path = "Assets/Prefabs/Particles/" + name + ".prefab";
        var p = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return p;
    }

    static void BuildParticles()
    {
        var crumbTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "Food/Crumbs0002.png");
        var crumbTex2 = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "Food/Crumbs0006.png");
        var glazeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "Food/Donut.png");
        var mCrumb = SpriteMaterial("FX_Crumb", crumbTex);
        var mCrumb2 = SpriteMaterial("FX_Crumb2", crumbTex2);
        var mGlaze = SpriteMaterial("FX_Glaze", glazeTex);

        // eating: little crumbs fly out of the mouth
        var go = new GameObject("FX_CrumbBurst");
        AddBurst(go, mCrumb, 10, new Vector2(1.5f, 3.5f), new Vector2(0.5f, 0.9f), new Vector2(0.35f, 0.6f), Color.white, new Color(1f, 0.85f, 0.7f), 0.15f, 0f);
        crumbBurst = SaveParticle(go, "FX_CrumbBurst");

        // growing: ring of yellow glaze + crumbs
        go = new GameObject("FX_GrowBurst");
        AddBurst(go, mGlaze, 22, new Vector2(3f, 5f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.8f), new Color(1f, 0.95f, 0.45f), Pink, 0.45f, 0f, true);
        var g2 = Child(go, "Crumbs");
        AddBurst(g2, mCrumb2, 12, new Vector2(1f, 3f), new Vector2(0.6f, 1f), new Vector2(0.5f, 0.9f), Color.white, Cream, 0.3f, 0f);
        growBurst = SaveParticle(go, "FX_GrowBurst");

        // touching an enemy: pink glaze splat
        go = new GameObject("FX_HitBurst");
        AddBurst(go, mGlaze, 14, new Vector2(2f, 5f), new Vector2(0.35f, 0.7f), new Vector2(0.3f, 0.5f), Pink, new Color(1f, 0.7f, 0.9f), 0.1f, 0f);
        hitBurst = SaveParticle(go, "FX_HitBurst");

        // player eaten
        go = new GameObject("FX_DeathBurst");
        AddBurst(go, mGlaze, 30, new Vector2(2f, 6f), new Vector2(0.6f, 1.2f), new Vector2(0.6f, 1.1f), Pink, new Color(1f, 0.6f, 0.85f), 0.3f, 0f);
        var d2 = Child(go, "Crumbs");
        AddBurst(d2, mCrumb, 24, new Vector2(1f, 4f), new Vector2(0.8f, 1.4f), new Vector2(0.7f, 1.2f), Color.white, new Color(0.9f, 0.7f, 0.5f), 0.3f, 0f);
        deathBurst = SaveParticle(go, "FX_DeathBurst");
    }

    // ------------------------------------------------------------------ 4. animations
    static AnimatorController playerCtrl, enemyCtrl, menuIdleCtrl, menuEnemyCtrl;

    static AnimationClip SpriteClip(string path, Sprite[] frames, float fps, bool loop, string spritePath = "Sprite")
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve(spritePath, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[loop ? 0 : frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void ScaleCurve(AnimationClip clip, string path, params Keyframe[] keys)
    {
        foreach (var axis in new[] { "x", "y" })
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale." + axis), new AnimationCurve(keys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"), AnimationCurve.Constant(0, keys[keys.Length - 1].time, 1));
    }

    static AnimatorController NewController(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static AnimatorStateTransition TriggerFromAny(AnimatorStateMachine sm, AnimatorState to, string trigger)
    {
        var t = sm.AddAnyStateTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.duration = 0; t.hasExitTime = false; t.canTransitionToSelf = true;
        return t;
    }

    static void ExitTo(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0f; t.hasFixedDuration = true;
    }

    static void BuildAnimations()
    {
        const string P = "Assets/Animations/Player/", E = "Assets/Animations/Enemy/", M = "Assets/Animations/Menu/";

        // ---- player
        var pIdle = SpriteClip(P + "Player_Idle.anim", new[] { 14, 16, 18, 20, 18, 16 }.Select(PlayerFrame).ToArray(), 7, true);
        var pBite = SpriteClip(P + "Player_Bite.anim", new[] { 0, 2, 4, 6, 12, 14 }.Select(PlayerFrame).ToArray(), 18, false);
        ScaleCurve(pBite, "Sprite", new Keyframe(0, 1), new Keyframe(0.12f, 1.12f), new Keyframe(0.33f, 1f));
        // animation events -> PlaySoundEvent.PlaySound("Eat") and SpawnParticleEvent.SpawnParticle() on the player
        AnimationUtility.SetAnimationEvents(pBite, new[]
        {
            new AnimationEvent { time = 0f, functionName = "PlaySound", stringParameter = "Eat" },
            new AnimationEvent { time = 0.12f, functionName = "SpawnParticle" },
        });
        var pGrow = SpriteClip(P + "Player_Grow.anim", new[] { 22, 24, 26, 28, 30 }.Select(PlayerFrame).ToArray(), 10, false);
        ScaleCurve(pGrow, "Sprite", new Keyframe(0, 1), new Keyframe(0.15f, 1.35f), new Keyframe(0.3f, 0.9f), new Keyframe(0.5f, 1f));

        playerCtrl = NewController(P + "Player.controller");
        playerCtrl.AddParameter("Bite", AnimatorControllerParameterType.Trigger);
        playerCtrl.AddParameter("Grow", AnimatorControllerParameterType.Trigger);
        var psm = playerCtrl.layers[0].stateMachine;
        var sIdle = psm.AddState("Idle"); sIdle.motion = pIdle;
        var sBite = psm.AddState("Bite"); sBite.motion = pBite;
        var sGrow = psm.AddState("Grow"); sGrow.motion = pGrow;
        psm.defaultState = sIdle;
        TriggerFromAny(psm, sBite, "Bite");
        TriggerFromAny(psm, sGrow, "Grow");
        ExitTo(sBite, sIdle); ExitTo(sGrow, sIdle);
        // state machine behaviours from the asset pack
        var growSound = sGrow.AddStateMachineBehaviour<PlaySoundEnter>();
        Set(growSound, "soundName", "Grow");
        var growFx = sGrow.AddStateMachineBehaviour<SpawnParticleOnEnter>();
        growFx.particlePrefab = growBurst; growFx.duration = 2f;
        EditorUtility.SetDirty(playerCtrl);

        // ---- enemy
        var eSpawn = SpriteClip(E + "Enemy_Spawn.anim", new[] { EnemyFrame(14) }, 6, false);
        ScaleCurve(eSpawn, "Sprite", new Keyframe(0, 0), new Keyframe(0.22f, 1.2f), new Keyframe(0.34f, 1f));
        var eIdle = SpriteClip(E + "Enemy_Idle.anim", new[] { 14, 16, 18, 16 }.Select(EnemyFrame).ToArray(), 6, true);
        var eBite = SpriteClip(E + "Enemy_Bite.anim", new[] { 0, 2, 4, 6, 12, 14 }.Select(EnemyFrame).ToArray(), 18, false);
        ScaleCurve(eBite, "Sprite", new Keyframe(0, 1), new Keyframe(0.12f, 1.15f), new Keyframe(0.33f, 1f));

        enemyCtrl = NewController(E + "Enemy.controller");
        enemyCtrl.AddParameter("Bite", AnimatorControllerParameterType.Trigger);
        var esm = enemyCtrl.layers[0].stateMachine;
        var esSpawn = esm.AddState("Spawn"); esSpawn.motion = eSpawn;
        var esIdle = esm.AddState("Idle"); esIdle.motion = eIdle;
        var esBite = esm.AddState("Bite"); esBite.motion = eBite;
        esm.defaultState = esSpawn;
        ExitTo(esSpawn, esIdle);
        TriggerFromAny(esm, esBite, "Bite");
        ExitTo(esBite, esIdle);
        var biteSound = esBite.AddStateMachineBehaviour<PlaySoundEnter>();
        Set(biteSound, "soundName", "EnemyBite");
        var biteFx = esBite.AddStateMachineBehaviour<SpawnParticleOnExit>();
        biteFx.particlePrefab = crumbBurst; biteFx.duration = 1.5f;
        EditorUtility.SetDirty(enemyCtrl);

        // ---- menu decoration (sprite on the same object)
        var mIdle = SpriteClip(M + "Menu_PlayerIdle.anim", new[] { 14, 16, 18, 20, 18, 16, 0, 2, 4, 6, 12, 14 }.Select(PlayerFrame).ToArray(), 7, true, "");
        menuIdleCtrl = NewController(M + "MenuPlayer.controller");
        menuIdleCtrl.layers[0].stateMachine.AddState("Idle").motion = mIdle;
        var mEnemy = SpriteClip(M + "Menu_EnemyIdle.anim", new[] { 14, 16, 18, 16, 14, 0, 2, 4, 6, 12 }.Select(EnemyFrame).ToArray(), 6, true, "");
        menuEnemyCtrl = NewController(M + "MenuEnemy.controller");
        menuEnemyCtrl.layers[0].stateMachine.AddState("Idle").motion = mEnemy;
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ 5. prefabs
    static GameObject playerPrefab, enemyPrefab, foodPrefab, indicatorPrefab;
    static PhysicsMaterial2D slippery;

    static void UnitBody(GameObject root, float radius, string spriteName, Sprite sprite, int order, out SpriteRenderer sr)
    {
        root.layer = L_UNITS;
        var trig = root.AddComponent<CircleCollider2D>();
        trig.isTrigger = true; trig.radius = radius;

        var spr = Child(root, spriteName);
        sr = spr.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingLayerName = "Units"; sr.sortingOrder = order;
    }

    static void SolidBody(GameObject root, float radius)
    {
        var body = Child(root, "Body");
        body.layer = L_BODIES;                      // Bodies only collide with Walls (see Physics 2D matrix)
        var c = body.AddComponent<CircleCollider2D>();
        c.radius = radius; c.sharedMaterial = slippery;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return p;
    }

    static readonly Rect PlayArea = new Rect(2.6f, 2.6f, MAP_W - 5.2f, MAP_H - 5.2f);

    static void BuildPrefabs()
    {
        slippery = LoadOrCreate("Assets/Physics/Slippery.physicsMaterial2D", () => new PhysicsMaterial2D("Slippery"));
        slippery.friction = 0f; slippery.bounciness = 0f;
        EditorUtility.SetDirty(slippery);

        // ---- Player
        var p = new GameObject("Player");
        p.tag = "Player";
        UnitBody(p, 0.42f, "Sprite", PlayerFrame(14), 10, out var pSr);
        SolidBody(p, 0.38f);
        Child(p, "Mouth", new Vector3(0, 0.15f, 0));
        var psh = p.AddComponent<PlayerSizeHandler>();
        Set(psh, "size", 1f); Set(psh, "pullStrength", 1f); Set(psh, "maxSize", 5f);
        var pc = p.AddComponent<PlayerController>();
        Set(pc, "movementSpeed", 5f); Set(pc, "rotationSpeed", 12f); Set(pc, "rb", p.GetComponent<Rigidbody2D>());
        var anim = p.AddComponent<Animator>(); anim.runtimeAnimatorController = playerCtrl;
        Set(psh, "anim", anim);
        p.AddComponent<PlaySoundEvent>();
        var spe = p.AddComponent<SpawnParticleEvent>();
        spe.particlePrefab = crumbBurst; spe.duration = 1.5f; spe.spawnPoint = p.transform.Find("Mouth");
        var flash = p.AddComponent<SpriteFlash>(); Set(flash, "target", pSr);
        var cf = p.AddComponent<ContactFeedback>();
        Set(cf, "particlePrefab", hitBurst); Set(cf, "flash", flash);
        playerPrefab = SavePrefab(p, "Assets/Prefabs/Characters/Player.prefab");

        // ---- Enemy
        var e = new GameObject("Enemy");
        e.tag = "Enemy";
        UnitBody(e, 0.42f, "Sprite", EnemyFrame(14), 5, out _);
        SolidBody(e, 0.38f);
        var esh = e.AddComponent<EnemySizeHandler>();
        Set(esh, "size", 1f); Set(esh, "pullStrength", 1f);
        var ai = e.AddComponent<EnemyAI>();
        Set(ai, "wanderDistance", 7f); Set(ai, "wanderSpeed", 2.3f); Set(ai, "idleTime", 1.2f); Set(ai, "playArea", PlayArea);
        var eAnim = e.AddComponent<Animator>(); eAnim.runtimeAnimatorController = enemyCtrl;
        Set(esh, "anim", eAnim);
        enemyPrefab = SavePrefab(e, "Assets/Prefabs/Characters/Enemy.prefab");

        // ---- Food (crumb art only fills ~30% of its 512px canvas, so the sprite is scaled up to match the collider)
        var f = new GameObject("Food");
        f.tag = "Food";
        UnitBody(f, 0.36f, "Sprite", CrumbSprites()[0], 2, out var fSr);
        fSr.transform.localScale = Vector3.one * 2.4f;
        var fsh = f.AddComponent<FoodSizeHandler>();
        Set(fsh, "size", 0.5f); Set(fsh, "pullStrength", 2f);
        var fv = f.AddComponent<FoodVisual>();
        Set(fv, "spriteRenderer", fSr);
        SetArray(fv, "variants", CrumbSprites().Cast<Object>().Append(Spr(ART + "Food/Donut.png")).ToArray());
        foodPrefab = SavePrefab(f, "Assets/Prefabs/Pickups/Food.prefab");

        // ---- Target indicator (HUD arrow pointing at an enemy)
        var ind = new GameObject("TargetIndicator");
        var ti = ind.AddComponent<TargetIndicator>();
        Set(ti, "hideDistance", 4.5f); Set(ti, "offset", 1.5f);
        var arrow = Child(ind, "Arrow");
        var aSr = arrow.AddComponent<SpriteRenderer>();
        aSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        aSr.sortingLayerName = "FX"; aSr.sortingOrder = 50;
        arrow.transform.localRotation = Quaternion.Euler(0, 0, 90); // the built-in arrow points down -> point along +X
        arrow.transform.localScale = Vector3.one * 3.2f;
        var icon = Child(ind, "Icon", new Vector3(-0.45f, 0, 0));
        var iSr = icon.AddComponent<SpriteRenderer>();
        iSr.sprite = EnemyFrame(14); iSr.sortingLayerName = "FX"; iSr.sortingOrder = 49;
        icon.transform.localScale = Vector3.one * 0.42f;
        icon.transform.localRotation = Quaternion.Euler(0, 0, -90);
        var tint = ind.AddComponent<IndicatorTint>();
        Set(tint, "indicator", ti);
        SetArray(tint, "renderers", new Object[] { aSr, iSr });
        indicatorPrefab = SavePrefab(ind, "Assets/Prefabs/UI/TargetIndicator.prefab");
    }

    // ------------------------------------------------------------------ UI helpers
    static Material outlineMat;

    static void MakeOutlineMaterial()
    {
        const string path = "Assets/Materials/TMP_DonutOutline.mat";
        var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
        if (src == null && font != null) src = font.material;
        if (src == null) return;
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
        outlineMat = new Material(src);
        outlineMat.EnableKeyword("OUTLINE_ON");
        outlineMat.SetFloat("_OutlineWidth", 0.22f);
        outlineMat.SetColor("_OutlineColor", DarkBrown);
        outlineMat.SetFloat("_FaceDilate", 0.15f);
        AssetDatabase.CreateAsset(outlineMat, path);
    }

    static RectTransform UI(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        var rt = UI(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return rt;
    }

    static Image Img(RectTransform rt, Color c, string builtin = "UI/Skin/UISprite.psd", bool sliced = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = builtin == null ? null : AssetDatabase.GetBuiltinExtraResource<Sprite>(builtin);
        img.type = sliced && builtin != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = c;
        return img;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, float size, Color col, TextAlignmentOptions align,
                               Vector2 anchor, Vector2 pos, Vector2 box, bool outline = true)
    {
        var rt = UI(name, parent, anchor, anchor, anchor, pos, box);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        if (outline && outlineMat != null) t.fontSharedMaterial = outlineMat;
        return t;
    }

    static Button Btn(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, PlaySoundEvent clickSound, Color? col = null)
    {
        var rt = UI(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, size);
        var img = Img(rt, col ?? Pink);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors; cb.highlightedColor = new Color(1f, 0.92f, 0.97f); cb.pressedColor = new Color(0.85f, 0.7f, 0.8f); cb.fadeDuration = 0.08f; b.colors = cb;
        rt.gameObject.AddComponent<ButtonJuice>();
        var t = Txt("Label", rt, label, size.y * 0.45f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.sizeDelta = Vector2.zero;
        if (clickSound != null) UnityEventTools.AddStringPersistentListener(b.onClick, clickSound.PlaySound, "Click"); // custom click sound
        return b;
    }

    static Canvas MakeCanvas(string name, out CanvasScaler scaler)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // scales across phone resolutions
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static void MakeEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static SoundManager MakeSoundManager(Transform parent, bool game)
    {
        var go = new GameObject("SoundManager");
        go.transform.SetParent(parent, false);
        var sm = go.AddComponent<SoundManager>();
        string music = "Assets/Audio/Music/music_kulluh_Exploring_The_Ocean_Floor_42.mp3";
        string lick1 = "Assets/Audio/SFX/zapsplat_cartoon_frog_lizard_tongue_lick_eat_insect_001_94311.mp3";
        string lick2 = "Assets/Audio/SFX/zapsplat_cartoon_frog_lizard_tongue_lick_eat_insect_002_94312.mp3";
        string squelch = "Assets/Audio/SFX/zapsplat_foley_liquid_gel_squelch_splat_short_47413.mp3";
        var list = new List<(string, string, float, float, bool)>
        {
            ("Music", music, 0.45f, 1f, true),
            ("Click", lick2, 0.7f, 1.8f, false),
        };
        if (game) list.AddRange(new[]
        {
            ("Eat", lick1, 0.9f, 1f, false),
            ("EnemyBite", lick2, 0.25f, 0.75f, false),
            ("Hit", squelch, 0.8f, 1f, false),
            ("Grow", squelch, 1f, 1.5f, false),
            ("Death", squelch, 1f, 0.55f, false),
            ("Win", lick1, 1f, 1.5f, false),
        });
        var so = new SerializedObject(sm);
        var arr = so.FindProperty("soundList");
        arr.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("name").stringValue = list[i].Item1;
            el.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(list[i].Item2);
            el.FindPropertyRelative("volume").floatValue = list[i].Item3;
            el.FindPropertyRelative("pitch").floatValue = list[i].Item4;
            el.FindPropertyRelative("loop").boolValue = list[i].Item5;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<PlayMusicOnStart>();
        return sm;
    }

    static void GlobalLight(Transform parent)
    {
        var go = new GameObject("Global Light 2D");
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light2D>();
        Set(l, "m_LightType", 4); // Global
        Set(l, "m_Intensity", 1f);
    }

    static GameObject Group(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    static void Panel(out RectTransform root, out RectTransform box, Transform canvas, string name, Vector2 boxSize, float dim)
    {
        root = Stretch(name, canvas);
        Img(root, new Color(0.12f, 0.05f, 0.01f, dim), null);
        box = UI("Box", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, boxSize);
        Img(box, new Color(0.45f, 0.22f, 0.07f, 0.97f));
        var inner = UI("Inner", box, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24, -24));
        Img(inner, new Color(1f, 0.78f, 0.55f, 1f));
    }

    // ------------------------------------------------------------------ 6. game scene
    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Managers
        var managers = Group("--- Managers ---");
        var gsm = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        gsm.transform.SetParent(managers.transform);
        var sound = MakeSoundManager(managers.transform, true);
        var spawnGo = new GameObject("SpawnManager"); spawnGo.transform.SetParent(managers.transform);
        var spawn = spawnGo.AddComponent<SpawnManager>();
        var flowGo = new GameObject("GameFlowManager"); flowGo.transform.SetParent(managers.transform);
        var flow = flowGo.AddComponent<GameFlowManager>();

        // --- Environment: Grid with separate Floor and Walls tilemap layers
        var env = Group("--- Environment ---");
        var grid = new GameObject("Grid"); grid.transform.SetParent(env.transform);
        grid.AddComponent<Grid>().cellSize = Vector3.one;
        var floorGo = Child(grid, "Floor");
        var floorTm = floorGo.AddComponent<Tilemap>();
        var floorR = floorGo.AddComponent<TilemapRenderer>(); floorR.sortingLayerName = "Floor"; floorR.mode = TilemapRenderer.Mode.Chunk;
        var wallGo = Child(grid, "Walls");
        wallGo.layer = L_WALLS;
        var wallTm = wallGo.AddComponent<Tilemap>();
        var wallR = wallGo.AddComponent<TilemapRenderer>(); wallR.sortingLayerName = "Walls"; wallR.mode = TilemapRenderer.Mode.Chunk;
        var wallRb = wallGo.AddComponent<Rigidbody2D>(); wallRb.bodyType = RigidbodyType2D.Static;
        var tmc = wallGo.AddComponent<TilemapCollider2D>();
        tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comp = wallGo.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
        PaintLevel(floorTm, wallTm, 16);
        comp.GenerateGeometry();
        GlobalLight(env.transform);

        var spawnPoint = new GameObject("PlayerSpawnPoint"); spawnPoint.transform.SetParent(env.transform);
        spawnPoint.transform.position = new Vector3(24.5f, 21.5f, 0);

        // --- Player
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = spawnPoint.transform.position;
        var psh = player.GetComponent<PlayerSizeHandler>();

        // --- Camera (perspective: CameraHandler zooms out by moving back on Z as the player grows)
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = false; cam.fieldOfView = 60f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.42f, 0.22f, 0.08f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -6.5f);
        var ch = camGo.AddComponent<CameraHandler>();
        Set(ch, "target", player); Set(ch, "baseZOffset", -6.5f);

        // --- Indicators parent follows the player
        var indParent = new GameObject("TargetIndicators");
        var ft = indParent.AddComponent<FollowTarget>();
        Set(ft, "target", player.transform);

        // --- Spawn manager settings
        Set(spawn, "spawnPoint", spawnPoint.transform);
        Set(spawn, "playerSizeHandler", psh);
        Set(spawn, "spawnOffsetMinimum", new Vector2(PlayArea.xMin, PlayArea.yMin));
        Set(spawn, "spawnOffsetMaximum", new Vector2(PlayArea.xMax, PlayArea.yMax));
        Set(spawn, "spawnTime", 1.0f);
        Set(spawn, "maxFood", 45);
        Set(spawn, "maxEnemies", 14);
        Set(spawn, "overlapLayers", (LayerMask)((1 << L_WALLS) | (1 << L_UNITS)));
        Set(spawn, "enemyPrefab", enemyPrefab);
        Set(spawn, "enemyMaximumSize", 1.5f);   // brief: max 1.5x the player's size
        Set(spawn, "enemyMinimumSize", 0.25f);  // brief: destroyed below 25% of the player's size
        Set(spawn, "targetIndicatorPrefab", indicatorPrefab);
        Set(spawn, "targetIndicatorParent", indParent.transform);
        Set(spawn, "targetIndicatorOffset", 1.5f);
        Set(spawn, "foodPrefab", foodPrefab);
        Set(spawn, "foodMaximumSize", 1.15f);   // a few crumbs are too big to eat until you grow
        Set(spawn, "foodMinimumSize", 0.2f);

        // --- UI
        var uiGroup = Group("--- UI ---");
        var canvas = MakeCanvas("Canvas", out _);
        canvas.transform.SetParent(uiGroup.transform);
        var uiSound = canvas.gameObject.AddComponent<PlaySoundEvent>();
        var uim = canvas.gameObject.AddComponent<UIManager>();
        var ct = canvas.transform;

        // HUD
        var hud = Stretch("HUD", ct);
        var sizeTxt = Txt("SizeText", hud, "Size: 1.00", 58, Cream, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(190, -60), new Vector2(380, 80));
        var barBg = UI("SatiationBar", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -105), new Vector2(300, 26));
        Img(barBg, new Color(0.25f, 0.1f, 0.03f, 0.85f));
        var fillRt = UI("Fill", barBg, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, new Vector2(-8, -8));
        var fill = Img(fillRt, Pink, "UI/Skin/UISprite.psd", false);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0f;
        Txt("SatiationLabel", hud, "next growth", 22, Cream, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(190, -150), new Vector2(300, 30));
        var growPop = Txt("GrowPopup", hud, "GROW!", 64, new Color(1f, 0.95f, 0.45f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(500, 100));
        var timerTxt = Txt("TimerText", hud, "Timer: 00:00", 62, Cream, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(520, 80));
        var objTxt = Txt("ObjectiveText", hud, "Survive: 10:00", 34, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(520, 50));
        var pauseBtn = Btn("PauseButton", hud, "II", new Vector2(1, 1), new Vector2(-90, -85), new Vector2(120, 120), uiSound);
        UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, gsm.TogglePause);
        var hint = Txt("DesktopControlsHint", hud, "W / Up - move forward     Mouse - steer     Esc - pause", 28, Cream,
                       TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1200, 50));
        var hudFx = hud.gameObject.AddComponent<SizeHudFeedback>();
        Set(hudFx, "player", psh); Set(hudFx, "satiationFill", fill); Set(hudFx, "sizeText", sizeTxt.rectTransform); Set(hudFx, "growPopup", growPop);

        // mobile joystick (Input System OnScreenStick -> <Gamepad>/leftStick -> Move action)
        var joyBg = UI("Joystick", hud, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(260, 260), new Vector2(330, 330));
        Img(joyBg, new Color(1f, 0.85f, 0.65f, 0.35f), "UI/Skin/Knob.psd", false);
        var knob = UI("Knob", joyBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
        Img(knob, new Color(1f, 0.49f, 0.85f, 0.9f), "UI/Skin/Knob.psd", false);
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.movementRange = 110f;
        Set(stick, "m_ControlPath", "<Gamepad>/leftStick");
        var mobile = hud.gameObject.AddComponent<MobileControls>();
        SetArray(mobile, "touchOnlyObjects", new Object[] { joyBg.gameObject });
        SetArray(mobile, "desktopOnlyObjects", new Object[] { hint.gameObject });

        // Pause menu
        Panel(out var pauseRoot, out var pauseBox, ct, "PauseMenu", new Vector2(640, 640), 0.6f);
        Txt("Title", pauseBox, "PAUSED", 90, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(560, 120));
        var resume = Btn("ResumeButton", pauseBox, "Resume", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(420, 105), uiSound);
        UnityEventTools.AddVoidPersistentListener(resume.onClick, uim.Resume);
        var restart = Btn("RestartButton", pauseBox, "Restart", new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(420, 105), uiSound);
        AddNav(restart, "Game", false);
        var quit = Btn("QuitButton", pauseBox, "Quit", new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(420, 105), uiSound, new Color(0.62f, 0.32f, 0.12f));
        AddNav(quit, "MainMenu", false);

        // End screens
        Panel(out var winRoot, out var winBox, ct, "WinPanel", new Vector2(760, 600), 0.65f);
        Txt("Title", winBox, "YOU SURVIVED!", 80, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(700, 120));
        var winStats = Txt("Stats", winBox, "", 40, Brown, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(680, 140), false);
        AddNav(Btn("PlayAgainButton", winBox, "Play Again", new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(420, 100), uiSound), "Game", false);
        AddNav(Btn("MainMenuButton", winBox, "Main Menu", new Vector2(0.5f, 0.5f), new Vector2(0, -215), new Vector2(420, 100), uiSound, new Color(0.62f, 0.32f, 0.12f)), "MainMenu", false);

        Panel(out var loseRoot, out var loseBox, ct, "LosePanel", new Vector2(760, 600), 0.65f);
        Txt("Title", loseBox, "GAME OVER", 86, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(700, 120));
        var loseStats = Txt("Stats", loseBox, "", 40, Brown, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(680, 140), false);
        AddNav(Btn("RestartButton", loseBox, "Try Again", new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(420, 100), uiSound), "Game", false);
        AddNav(Btn("MainMenuButton", loseBox, "Main Menu", new Vector2(0.5f, 0.5f), new Vector2(0, -215), new Vector2(420, 100), uiSound, new Color(0.62f, 0.32f, 0.12f)), "MainMenu", false);

        // UIManager (provided) wiring
        Set(uim, "hud", hud.gameObject); Set(uim, "playerSizeHandler", psh);
        Set(uim, "tmpSize", sizeTxt); Set(uim, "tmpTimer", timerTxt); Set(uim, "pauseMenu", pauseRoot.gameObject);
        pauseRoot.gameObject.SetActive(false);
        winRoot.gameObject.SetActive(false); loseRoot.gameObject.SetActive(false);

        Set(flow, "uiManager", uim); Set(flow, "player", psh);
        Set(flow, "winPanel", winRoot.gameObject); Set(flow, "losePanel", loseRoot.gameObject);
        Set(flow, "winStats", winStats); Set(flow, "loseStats", loseStats); Set(flow, "objectiveText", objTxt);
        Set(flow, "deathParticlePrefab", deathBurst);
        Set(flow, "surviveSeconds", 600f);

        MakeEventSystem();
        GameObject.Find("EventSystem").transform.SetParent(uiGroup.transform);
        indParent.transform.SetParent(uiGroup.transform);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    // buttons use the provided SceneNavigation script
    static void AddNav(Button b, string sceneName, bool exit)
    {
        var nav = b.gameObject.AddComponent<SceneNavigation>();
        Set(nav, "sceneName", sceneName);
        if (exit) UnityEventTools.AddVoidPersistentListener(b.onClick, nav.ExitGame);
        else UnityEventTools.AddVoidPersistentListener(b.onClick, nav.LoadScene);
    }

    // ------------------------------------------------------------------ 7. main menu
    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var managers = Group("--- Managers ---");
        MakeSoundManager(managers.transform, false);

        // background: a slice of the donut level with a couple of animated donuts
        var env = Group("--- Background ---");
        var grid = new GameObject("Grid"); grid.transform.SetParent(env.transform); grid.AddComponent<Grid>();
        var fGo = Child(grid, "Floor"); var fTm = fGo.AddComponent<Tilemap>(); fGo.AddComponent<TilemapRenderer>().sortingLayerName = "Floor";
        var wGo = Child(grid, "Walls"); var wTm = wGo.AddComponent<Tilemap>(); wGo.AddComponent<TilemapRenderer>().sortingLayerName = "Walls";
        PaintLevel(fTm, wTm, 16);
        GlobalLight(env.transform);

        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.42f, 0.22f, 0.08f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(35.5f, 22.5f, -10f);

        var deco = new GameObject("MenuPlayer"); deco.transform.SetParent(env.transform);
        deco.transform.position = new Vector3(29.5f, 19.8f, 0); deco.transform.localScale = Vector3.one * 2.6f;
        var dsr = deco.AddComponent<SpriteRenderer>(); dsr.sprite = PlayerFrame(14); dsr.sortingLayerName = "Units"; dsr.sortingOrder = 5;
        deco.AddComponent<Animator>().runtimeAnimatorController = menuIdleCtrl;
        foreach (var (pos, s) in new[] { (new Vector3(41.5f, 19.3f, 0), 1.6f), (new Vector3(44.6f, 23.8f, 0), 0.9f), (new Vector3(26f, 25.5f, 0), 0.7f) })
        {
            var en = new GameObject("MenuEnemy"); en.transform.SetParent(env.transform);
            en.transform.position = pos; en.transform.localScale = Vector3.one * s;
            var esr = en.AddComponent<SpriteRenderer>(); esr.sprite = EnemyFrame(14); esr.sortingLayerName = "Units";
            en.AddComponent<Animator>().runtimeAnimatorController = menuEnemyCtrl;
        }
        var crumbs = CrumbSprites();
        for (int i = 0; i < 6; i++)
        {
            var c = new GameObject("MenuCrumb"); c.transform.SetParent(env.transform);
            c.transform.position = new Vector3(31.5f + i * 1.7f, 18.4f + (i % 2) * 0.8f, 0); c.transform.localScale = Vector3.one * 1.3f;
            c.transform.rotation = Quaternion.Euler(0, 0, i * 57f);
            var csr = c.AddComponent<SpriteRenderer>(); csr.sprite = crumbs[i % crumbs.Length]; csr.sortingLayerName = "Units";
        }

        var uiGroup = Group("--- UI ---");
        var canvas = MakeCanvas("Canvas", out _);
        canvas.transform.SetParent(uiGroup.transform);
        var uiSound = canvas.gameObject.AddComponent<PlaySoundEvent>();
        var ct = canvas.transform;
        var shade = Stretch("Shade", ct); Img(shade, new Color(0.15f, 0.06f, 0.01f, 0.35f), null);
        Txt("Title", ct, "DONUT DEVOUR", 150, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1500, 190));
        Txt("Tagline", ct, "Eat smaller snacks. Avoid bigger donuts. Survive 10 minutes.", 44, Cream, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(0, -265), new Vector2(1500, 70));
        var play = Btn("PlayButton", ct, "PLAY", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(460, 130), uiSound);
        AddNav(play, "Game", false);
        var exit = Btn("ExitButton", ct, "EXIT", new Vector2(0.5f, 0.5f), new Vector2(0, -175), new Vector2(460, 130), uiSound, new Color(0.62f, 0.32f, 0.12f));
        AddNav(exit, "", true);
        Txt("Controls", ct, "PC: W to move, mouse to steer, Esc to pause     Phone: drag the joystick to steer & move, II to pause\n" +
                            "Red arrows = bigger enemies (run!)   Green arrows = smaller enemies (eat them!)", 30, Cream,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1700, 110));
        Txt("Credit", ct, "Neo Nti  33HA2608708  HFGE1181 SA1", 24, new Color(1f, 0.85f, 0.62f, 0.7f), TextAlignmentOptions.Right,
            new Vector2(1, 0), new Vector2(-260, 25), new Vector2(500, 40), false);
        MakeEventSystem();
        GameObject.Find("EventSystem").transform.SetParent(uiGroup.transform);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ------------------------------------------------------------------ 8. build + player settings
    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
        };
        PlayerSettings.companyName = "NeoNti";
        PlayerSettings.productName = "Donut Devour";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.NeoNti.DonutDevour");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "Characters/Player/Player0000.png");
        if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("SA1/Build Android APK")]
    public static void BuildApk()
    {
        ConfigureProject();
        Directory.CreateDirectory("APK");
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" },
            locationPathName = "APK/Neo_Nti_33HA2608708_HFGE1181_SA1.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("SA1: APK build " + report.summary.result + " (" + report.summary.totalSize / (1024 * 1024) + " MB) errors=" + report.summary.totalErrors);
    }
}
