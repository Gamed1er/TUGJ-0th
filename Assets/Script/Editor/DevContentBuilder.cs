using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// 一鍵產生「佔位美術 + Prefab + 道具資產 + Title/Game 測試場景」。
/// 選單：Tools/TUGJ/Build Test Content。
/// 注意：會覆蓋 Assets/Art/Placeholder、Assets/Prefabs、Title.unity、Game.unity。美術換圖後就不要再跑了。
/// 這只是開發輔助工具，正式關卡請在 Game 場景直接用 Tile Palette 畫。
/// </summary>
public static class DevContentBuilder
{
    const string ArtDir = "Assets/Art/Placeholder";
    const string TileDir = "Assets/Art/Tiles";
    const string PrefabDir = "Assets/Prefabs";
    const string ItemDir = "Assets/Resources/Items";
    const string SceneDir = "Assets/Scenes";
    const int PPU = 32;

    static Font font;

    [MenuItem("Tools/TUGJ/Build Test Content")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var d in new[] { ArtDir, TileDir, PrefabDir, PrefabDir + "/UI", ItemDir, SceneDir, "Assets/Art/Physics" })
            Directory.CreateDirectory(d);

        var art = BuildArt();
        AssetDatabase.SaveAssets();
        var prefabs = BuildPrefabs(art);
        BuildItems(art, prefabs);
        AssetDatabase.SaveAssets();
        BuildTitleScene();
        BuildGameScene(art, prefabs);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{SceneDir}/Title.unity", true),
            new EditorBuildSettingsScene($"{SceneDir}/Game.unity", true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[DevContentBuilder] 完成：Title / Game 場景已產生並加入 Build Profiles");
    }

    // =====================================================================
    // 佔位美術
    // =====================================================================

    class Art
    {
        public Sprite square, slopeR, slopeL, spike, circle, ring, player, savePoint;
        public Sprite battery, plate, breakable, lever, drone, iconRocket, iconBattery, iconBlackHole, iconDrone;
        public Sprite[] playerFrames;
        public Material particle;
        public TileBase groundTile, slopeRTile, slopeLTile, spikeTile;
    }

    static Art BuildArt()
    {
        var a = new Art();
        a.square = MakeSprite("square", 32, 32, (x, y) => true, Color.white);
        a.slopeR = MakeSprite("slope_right", 32, 32, (x, y) => y <= x, Color.white,
            new[] { new Vector2(-16, -16), new Vector2(16, -16), new Vector2(16, 16) });
        a.slopeL = MakeSprite("slope_left", 32, 32, (x, y) => y <= 31 - x, Color.white,
            new[] { new Vector2(-16, -16), new Vector2(16, -16), new Vector2(-16, 16) });
        a.spike = MakeSprite("spike", 32, 32, (x, y) => y < 20 && Mathf.Abs((x % 16) - 7.5f) < (20 - y) * 0.4f, Color.white,
            new[] { new Vector2(-14, -16), new Vector2(14, -16), new Vector2(14, 0), new Vector2(-14, 0) });
        a.circle = MakeSprite("circle", 64, 64, (x, y) => (new Vector2(x - 31.5f, y - 31.5f)).magnitude < 31f, Color.white);
        a.player = MakeSprite("player", 26, 48, (x, y) => true, new Color(1f, 0.55f, 0.25f));
        a.savePoint = MakeSprite("save_point", 32, 64, (x, y) => x > 8 && x < 24 || y > 52, Color.white, null, new Vector2(0.5f, 0f));
        // 直徑 1 單位的圓環 (黑洞引力範圍)
        a.ring = MakeSprite("ring", 128, 128, (x, y) => { float d = new Vector2(x - 63.5f, y - 63.5f).magnitude; return d < 63f && d > 59f; },
            Color.white, null, null, 128);
        a.battery = MakeSprite("battery", 32, 32, (x, y) => (x >= 1 && x <= 30 && y >= 1 && y <= 26) || (x >= 11 && x <= 20 && y > 26 && y <= 30),
            Color.white);
        a.plate = MakeSprite("pressure_plate", 32, 8, (x, y) => x >= 2 && x <= 29, Color.white, null, new Vector2(0.5f, 0f));
        a.lever = MakeSprite("lever", 32, 32, (x, y) => (y < 6 && x > 6 && x < 25) || (Mathf.Abs((x - 16) - (y - 6) * 0.6f) < 2.5f && y >= 6 && y < 28),
            Color.white, null, new Vector2(0.5f, 0f));
        // 無人機：機身 + 兩側螺旋槳
        a.drone = MakeSprite("drone", 32, 18, (x, y) => (y >= 3 && y <= 10 && x >= 8 && x <= 23) || (y >= 13 && y <= 15 && (x <= 11 || x >= 20)) || (y >= 10 && y <= 13 && (x == 6 || x == 25)),
            Color.white);
        a.iconDrone = MakeSprite("icon_drone", 32, 32, (x, y) => (y >= 10 && y <= 18 && x >= 9 && x <= 22) || (y >= 22 && y <= 24 && (x <= 12 || x >= 19)) || (y >= 18 && y <= 22 && (x == 6 || x == 25)),
            new Color(0.3f, 0.9f, 1f));
        a.breakable = MakeSprite("breakable", 32, 32, (x, y) => !(Mathf.Abs(x - y) <= 1 || (Mathf.Abs(x + y - 31) <= 1 && x > 16)), Color.white);
        a.iconRocket = MakeSprite("icon_rocket", 32, 32, (x, y) => (y > 8 && y < 26 && Mathf.Abs(x - 15.5f) < 6) || (y >= 26 && Mathf.Abs(x - 15.5f) < 31 - y) || (y <= 8 && y > 2 && Mathf.Abs(x - 15.5f) < 3),
            new Color(0.4f, 0.7f, 1f));
        a.iconBattery = MakeSprite("icon_battery", 32, 32, (x, y) => (x > 6 && x < 25 && y > 2 && y < 25) || (x > 11 && x < 20 && y >= 25 && y < 29),
            new Color(1f, 0.85f, 0.25f));
        a.iconBlackHole = MakeSprite("icon_black_hole", 32, 32, (x, y) => { float d = new Vector2(x - 15.5f, y - 15.5f).magnitude; return d < 14f && (d < 7f || d > 10f); },
            new Color(0.7f, 0.3f, 1f));

        a.playerFrames = SlicePlayerSheet();
        a.particle = MakeParticleMaterial(a.circle);

        a.groundTile = MakeTile("Ground", a.square, new Color(0.35f, 0.3f, 0.28f), Tile.ColliderType.Grid);
        a.slopeRTile = MakeTile("SlopeRight", a.slopeR, new Color(0.35f, 0.3f, 0.28f), Tile.ColliderType.Sprite);
        a.slopeLTile = MakeTile("SlopeLeft", a.slopeL, new Color(0.35f, 0.3f, 0.28f), Tile.ColliderType.Sprite);
        a.spikeTile = MakeTile("Spike", a.spike, new Color(0.9f, 0.2f, 0.2f), Tile.ColliderType.Sprite);
        return a;
    }

    static Sprite MakeSprite(string name, int w, int h, System.Func<int, int, bool> fill, Color color,
        Vector2[] physicsOutline = null, Vector2? pivot = null, int ppu = PPU)
    {
        string path = $"{ArtDir}/{name}.png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, fill(x, y) ? color : Color.clear);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = pivot.HasValue ? (int)SpriteAlignment.Custom : (int)SpriteAlignment.Center;
        settings.spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
        settings.spriteGenerateFallbackPhysicsShape = true;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();

        if (physicsOutline != null) SetPhysicsShape(imp, physicsOutline);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    const string PlayerSheetPath = "Assets/Art/protagnist/pro-Sheet.png";

    /// <summary>
    /// 角色圖 pro-Sheet.png：200x40，4 格 (每格 50x40)，第 1~4 格 = 走路、第 2 格 = 站立。
    /// 角色本體約 22x34 像素，PPU 24 → 約 1.4 格高，對齊玩家碰撞體 (高 1.5)；pivot 讓腳底貼齊碰撞體底部。
    /// </summary>
    static Sprite[] SlicePlayerSheet()
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(PlayerSheetPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 24;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(imp);
        provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[4];
        for (int i = 0; i < 4; i++)
            rects[i] = new SpriteRect
            {
                name = $"pro_{i + 1}",
                rect = new Rect(i * 50, 0, 50, 40),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.475f),
                spriteID = GUID.Generate(),
            };
        provider.SetSpriteRects(rects);
        provider.Apply();
        imp.SaveAndReimport();

        var frames = new Sprite[4];
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(PlayerSheetPath))
            if (o is Sprite sp && sp.name.StartsWith("pro_")) frames[int.Parse(sp.name.Substring(4)) - 1] = sp;
        return frames;
    }

    /// <summary>粒子用材質 (URP 2D 的 Sprite-Unlit + 圓形貼圖，支援粒子顏色/透明度)。</summary>
    static Material MakeParticleMaterial(Sprite circle)
    {
        string path = $"{ArtDir}/Particle.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.mainTexture = circle.texture;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>精確指定 Sprite 的碰撞外形 (像素座標，以中心為原點)，斜坡才會是完美 45 度。</summary>
    static void SetPhysicsShape(TextureImporter imp, Vector2[] outline)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(imp);
        provider.InitSpriteEditorDataProvider();
        var physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        var rect = provider.GetSpriteRects()[0];
        physics.SetOutlines(rect.spriteID, new List<Vector2[]> { outline });
        provider.Apply();
        imp.SaveAndReimport();
    }

    static TileBase MakeTile(string name, Sprite sprite, Color color, Tile.ColliderType collider)
    {
        string path = $"{TileDir}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = collider;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // =====================================================================
    // Prefabs
    // =====================================================================

    class Prefabs
    {
        public GameObject player, savePoint, lever, battery, door, plate, breakable, blackHole, drone, platform;
        public InventorySlotUI slot;
        public MapNodeUI mapNode;
    }

    static Prefabs BuildPrefabs(Art art)
    {
        var p = new Prefabs();
        int groundLayer = LayerMask.NameToLayer("Ground");

        // --- 電池爆炸特效 (生成後自己播放、播完自己刪除) ---
        var boom = MakeParticles("ExplosionFx", null, art.particle, 25);
        {
            var main = boom.main;
            main.playOnAwake = true;
            main.loop = false;
            main.duration = 0.5f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f); // 約等於爆炸半徑 2.5 格
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new Color(1f, 0.75f, 0.2f);
            var emission = boom.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            var shape = boom.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            var col = boom.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.1f), 1f, 0f);
            var size = boom.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));
        }
        var boomPrefab = SavePrefab(boom.gameObject, "ExplosionFx");

        // --- 電池 (Ground 圖層：玩家可以站上去、可以推) ---
        const string batteryMatPath = "Assets/Art/Physics/Battery.physicsMaterial2D";
        var batteryMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(batteryMatPath);
        if (batteryMat == null)
        {
            // 摩擦力大 → 推完不會滑。玩家材質摩擦力 0 (Mean 合併 = 0)，所以推/貼著電池不會卡住
            batteryMat = new PhysicsMaterial2D("Battery") { friction = 5f, bounciness = 0f };
            AssetDatabase.CreateAsset(batteryMat, batteryMatPath);
        }
        var battery = new GameObject("Battery") { layer = groundLayer };
        var batSr = battery.AddComponent<SpriteRenderer>();
        batSr.sprite = art.battery; batSr.color = new Color(1f, 0.85f, 0.25f); batSr.sortingOrder = 6;
        var batCol = battery.AddComponent<BoxCollider2D>();
        batCol.size = new Vector2(0.96f, 0.96f);
        batCol.sharedMaterial = batteryMat;
        var batRb = battery.AddComponent<Rigidbody2D>();
        batRb.gravityScale = 3f;
        batRb.freezeRotation = true;
        batRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        batRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        battery.AddComponent<Battery>().explosionFx = boomPrefab;
        p.battery = SavePrefab(battery, "Battery");

        // --- 門 (1x1，場景裡用 Scale Y 拉高) ---
        var door = new GameObject("Door") { layer = groundLayer };
        var doorSr = door.AddComponent<SpriteRenderer>();
        doorSr.sprite = art.square; doorSr.color = new Color(0.3f, 0.45f, 0.75f); doorSr.sortingOrder = 4;
        var doorCol = door.AddComponent<BoxCollider2D>();
        var doorComp = door.AddComponent<Door>();
        Set(doorComp, "solid", doorCol);
        Set(doorComp, "visual", doorSr);
        p.door = SavePrefab(door, "Door");

        // --- 壓力板 (原點 = 地面表面) ---
        var plate = new GameObject("PressurePlate");
        var plateVisual = new GameObject("Visual");
        plateVisual.transform.SetParent(plate.transform, false);
        var plateSr = plateVisual.AddComponent<SpriteRenderer>();
        plateSr.sprite = art.plate; plateSr.sortingOrder = 4;
        var plateCol = plate.AddComponent<BoxCollider2D>();
        plateCol.isTrigger = true; plateCol.size = new Vector2(0.9f, 0.4f); plateCol.offset = new Vector2(0f, 0.2f);
        var plateComp = plate.AddComponent<PressurePlate>();
        Set(plateComp, "visual", plateSr);
        p.plate = SavePrefab(plate, "PressurePlate");

        // --- 可破壞物件 ---
        var brk = new GameObject("Breakable") { layer = groundLayer };
        var brkSr = brk.AddComponent<SpriteRenderer>();
        brkSr.sprite = art.breakable; brkSr.color = new Color(0.65f, 0.5f, 0.4f); brkSr.sortingOrder = 4;
        brk.AddComponent<BoxCollider2D>();
        brk.AddComponent<Breakable>();
        p.breakable = SavePrefab(brk, "Breakable");

        // --- 黑洞 ---
        var hole = new GameObject("BlackHole") { layer = 2 };
        var core = new GameObject("Core");
        core.transform.SetParent(hole.transform, false);
        core.transform.localScale = Vector3.one * 0.35f; // circle 直徑 2 → 0.7
        var coreSr = core.AddComponent<SpriteRenderer>();
        coreSr.sprite = art.circle; coreSr.color = new Color(0.25f, 0.05f, 0.35f); coreSr.sortingOrder = 20;
        var range = new GameObject("Range");
        range.transform.SetParent(hole.transform, false);
        var rangeSr = range.AddComponent<SpriteRenderer>();
        rangeSr.sprite = art.ring; rangeSr.color = new Color(0.7f, 0.3f, 1f, 0.5f); rangeSr.sortingOrder = 19;
        var holeComp = hole.AddComponent<BlackHole>();
        holeComp.hitMask = 1 << groundLayer;
        holeComp.rangeIndicator = range;
        // 吸入粒子：圓形外圈生成往中心移動，越靠近中心越明顯，到中心消失 (半徑/壽命由 BlackHole 依 pullRadius 設定)
        var pull = MakeParticles("PullFx", hole.transform, art.particle, 18);
        {
            var main = pull.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.375f);
            main.startColor = new Color(0.3f, 0.05f, 0.4f);
            var emission = pull.emission; emission.rateOverTime = 60f;
            var shape = pull.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radiusThickness = 0f; // 只從外圈生成
            var col = pull.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(0.35f, 0.05f, 0.5f), new Color(0.08f, 0f, 0.12f), 0f, 1f);
            var size = pull.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
        }
        holeComp.pullFx = pull;
        p.blackHole = SavePrefab(hole, "BlackHole");

        // --- 玩家 ---
        const string matPath = "Assets/Art/Physics/PlayerNoFriction.physicsMaterial2D";
        var noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, matPath);
        }

        var player = new GameObject("Player") { tag = "Player" };
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = art.playerFrames[1]; psr.sortingOrder = 10;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cap = player.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(0.8f, 1.5f);
        cap.sharedMaterial = noFriction;
        var pc = player.AddComponent<PlayerController>();
        pc.groundMask = 1 << groundLayer;
        pc.sprite = psr;
        player.AddComponent<Inventory>();
        player.AddComponent<PlayerInteractor>();
        player.AddComponent<PlayerItemUser>();
        var rocket = player.AddComponent<PlayerRocket>();
        var anim = player.AddComponent<PlayerAnimator>();
        Set(anim, "sprite", psr);
        Set(anim, "walkFrames", art.playerFrames);
        Set(anim, "idleFrame", art.playerFrames[1]);
        // 火箭噴射：腳下往下噴的淺灰色粒子
        var thrust = MakeParticles("ThrustFx", player.transform, art.particle, 9);
        thrust.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        thrust.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Cone 預設朝 +Z，轉成朝下
        {
            var main = thrust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.225f, 0.45f);
            main.startColor = new Color(0.82f, 0.82f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = thrust.emission; emission.rateOverTime = 90f;
            var shape = thrust.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f; shape.radius = 0.15f;
            var col = thrust.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(0.85f, 0.85f, 0.88f), new Color(0.6f, 0.6f, 0.62f), 0.9f, 0f);
        }
        Set(rocket, "thrustFx", thrust);
        p.player = SavePrefab(player, "Player");

        // --- 節點 (隱形自動存檔點；範圍蓋住整條通道高度，玩家飛過也會觸發) ---
        var sp = new GameObject("SavePoint") { layer = 2 };
        var spCol = sp.AddComponent<BoxCollider2D>();
        spCol.isTrigger = true; spCol.size = new Vector2(1f, 14f); spCol.offset = new Vector2(0f, 7f);
        var spComp = sp.AddComponent<SavePoint>();
        Set(spComp, "spawnOffset", new Vector2(0f, 0.8f));
        Set(spComp, "id", "");
        p.savePoint = SavePrefab(sp, "SavePoint");

        // --- 拉桿 (原點 = 地面表面) ---
        var lever = new GameObject("Lever") { layer = 2 };
        var leverVisual = new GameObject("Visual");
        leverVisual.transform.SetParent(lever.transform, false);
        var leverSr = leverVisual.AddComponent<SpriteRenderer>();
        leverSr.sprite = art.lever; leverSr.sortingOrder = 5;
        var leverCol = lever.AddComponent<BoxCollider2D>();
        leverCol.isTrigger = true; leverCol.size = new Vector2(1.5f, 1.5f); leverCol.offset = new Vector2(0f, 0.75f);
        var leverComp = lever.AddComponent<Lever>();
        Set(leverComp, "visual", leverSr);
        p.lever = SavePrefab(lever, "Lever");

        // --- 無人機 (Default 圖層：黑洞會穿過；跟玩家/電池的碰撞由 Drone 程式關掉) ---
        var drone = new GameObject("Drone");
        var droneSr = drone.AddComponent<SpriteRenderer>();
        droneSr.sprite = art.drone; droneSr.sortingOrder = 11;
        var droneCol = drone.AddComponent<BoxCollider2D>();
        droneCol.size = new Vector2(0.9f, 0.45f);
        var droneRb = drone.AddComponent<Rigidbody2D>();
        droneRb.gravityScale = 0f;
        droneRb.freezeRotation = true;
        droneRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        droneRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var droneComp = drone.AddComponent<Drone>();
        droneComp.blockMask = 1 << groundLayer;
        droneComp.visual = droneSr;
        p.drone = SavePrefab(drone, "Drone");

        // --- 軌道平台 (根物件不動；Body 是會動的平台，預設 3x0.5；End 是終點) ---
        var platform = new GameObject("MovingPlatform");
        var track = new GameObject("Track");
        track.transform.SetParent(platform.transform, false);
        var trackSr = track.AddComponent<SpriteRenderer>();
        trackSr.sprite = art.square; trackSr.color = new Color(0.25f, 0.25f, 0.3f); trackSr.sortingOrder = 1;
        var pBody = new GameObject("Body") { layer = groundLayer };
        pBody.transform.SetParent(platform.transform, false);
        pBody.transform.localScale = new Vector3(3f, 0.5f, 1f);
        var pBodySr = pBody.AddComponent<SpriteRenderer>();
        pBodySr.sprite = art.square; pBodySr.color = new Color(0.55f, 0.6f, 0.7f); pBodySr.sortingOrder = 4;
        pBody.AddComponent<BoxCollider2D>();
        var pRb = pBody.AddComponent<Rigidbody2D>();
        pRb.bodyType = RigidbodyType2D.Kinematic;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pEnd = new GameObject("End");
        pEnd.transform.SetParent(platform.transform, false);
        pEnd.transform.localPosition = new Vector3(0f, 3f, 0f);
        var mp = platform.AddComponent<MovingPlatform>();
        Set(mp, "body", pRb);
        Set(mp, "end", pEnd.transform);
        Set(mp, "track", trackSr);
        mp.blockMask = 1 << groundLayer;
        p.platform = SavePrefab(platform, "MovingPlatform");

        // --- UI：物品欄格子 ---
        var slot = NewUI("InventorySlot", null, new Vector2(96, 96));
        var slotRaycast = slot.gameObject.AddComponent<Image>();
        slotRaycast.color = new Color(0, 0, 0, 0);
        var selected = NewImage("SelectedFrame", slot, new Color(1f, 0.85f, 0.2f));
        Stretch(selected.rectTransform, -6);
        var bg = NewImage("Background", slot, new Color(0.1f, 0.1f, 0.12f, 0.85f));
        Stretch(bg.rectTransform, 0);
        var slotIcon = NewImage("Icon", slot, Color.white);
        Stretch(slotIcon.rectTransform, 14);
        slotIcon.preserveAspect = true;
        var key = NewText("Key", slot, "1", 20, TextAnchor.UpperLeft);
        Stretch(key.rectTransform, 6);
        var dur = NewText("Uses", slot, "", 24, TextAnchor.LowerRight);
        Stretch(dur.rectTransform, 6);
        var slotUI = slot.gameObject.AddComponent<InventorySlotUI>();
        Set(slotUI, "icon", slotIcon);
        Set(slotUI, "usesText", dur);
        Set(slotUI, "keyText", key);
        Set(slotUI, "selectedFrame", selected.gameObject);
        Set(slotUI, "background", bg);
        p.slot = SavePrefab(slot.gameObject, "UI/InventorySlot").GetComponent<InventorySlotUI>();

        // --- UI：地圖節點 ---
        var node = NewUI("MapNode", null, new Vector2(28, 28));
        var dot = node.gameObject.AddComponent<Image>();
        dot.sprite = art.circle;
        var nodeLabel = NewText("Label", node, "???", 22, TextAnchor.UpperCenter);
        nodeLabel.rectTransform.sizeDelta = new Vector2(200, 30);
        nodeLabel.rectTransform.anchoredPosition = new Vector2(0, -32);
        var nodeUI = node.gameObject.AddComponent<MapNodeUI>();
        Set(nodeUI, "dot", dot);
        Set(nodeUI, "label", nodeLabel);
        p.mapNode = SavePrefab(node.gameObject, "UI/MapNode").GetComponent<MapNodeUI>();

        return p;
    }

    static ParticleSystem MakeParticles(string name, Transform parent, Material mat, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = sortingOrder;
        return ps;
    }

    static Gradient Fade(Color from, Color to, float alphaFrom, float alphaTo)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                  new[] { new GradientAlphaKey(alphaFrom, 0f), new GradientAlphaKey(alphaTo, 1f) });
        return g;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // =====================================================================
    // 道具資產
    // =====================================================================

    static RocketItem rocketItem;
    static BatteryItem batteryItem;
    static BlackHoleGunItem blackHoleItem;
    static DroneItem droneItem;

    static void BuildItems(Art art, Prefabs p)
    {
        rocketItem = LoadOrCreate<RocketItem>($"{ItemDir}/Rocket.asset");
        rocketItem.id = "rocket"; rocketItem.displayName = "火箭推進器"; rocketItem.icon = art.iconRocket;
        rocketItem.startingUses = 3; rocketItem.chargeTime = 0.15f;
        rocketItem.description = "長按右鍵往上飛，放開或藍條耗盡就結束。";
        EditorUtility.SetDirty(rocketItem);

        batteryItem = LoadOrCreate<BatteryItem>($"{ItemDir}/Battery.asset");
        batteryItem.id = "battery"; batteryItem.displayName = "電池倉"; batteryItem.icon = art.iconBattery;
        batteryItem.startingUses = 5; batteryItem.batteryPrefab = p.battery;
        batteryItem.description = "在前方生成一顆可推動的電池，可以壓住壓力板。";
        EditorUtility.SetDirty(batteryItem);

        blackHoleItem = LoadOrCreate<BlackHoleGunItem>($"{ItemDir}/BlackHoleGun.asset");
        blackHoleItem.id = "black_hole_gun"; blackHoleItem.displayName = "黑洞槍"; blackHoleItem.icon = art.iconBlackHole;
        blackHoleItem.startingUses = 3; blackHoleItem.blackHolePrefab = p.blackHole.GetComponent<BlackHole>();
        blackHoleItem.description = "往滑鼠方向射出黑洞，再按一次固定、固定後再按收回。";
        EditorUtility.SetDirty(blackHoleItem);

        droneItem = LoadOrCreate<DroneItem>($"{ItemDir}/Drone.asset");
        droneItem.id = "drone"; droneItem.displayName = "無人機"; droneItem.icon = art.iconDrone;
        droneItem.startingUses = 3; droneItem.dronePrefab = p.drone.GetComponent<Drone>();
        droneItem.description = "右鍵召喚並操控 (WASD)，E 拉拉桿 / 吊起放下電池，用完就沒電。再按右鍵退出操控。";
        EditorUtility.SetDirty(droneItem);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
        return a;
    }

    // =====================================================================
    // Title 場景
    // =====================================================================

    static void BuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        var canvas = MakeCanvas();
        MakeEventSystem();

        var title = NewText("Title", canvas, "TUGJ-0th (暫定標題)", 72, TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = new Vector2(0, 320);
        title.rectTransform.sizeDelta = new Vector2(1200, 120);

        var slots = new TitleSlotUI[SaveSystem.SlotCount];
        for (int i = 0; i < slots.Length; i++)
        {
            var row = NewUI($"Slot{i + 1}", canvas, new Vector2(700, 110));
            row.anchoredPosition = new Vector2(0, 120 - i * 140);

            var play = NewButton("PlayButton", row, new Vector2(540, 110), out var playLabel, 32);
            play.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 0);
            var del = NewButton("DeleteButton", row, new Vector2(140, 110), out var delLabel, 28);
            del.GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);
            del.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f);

            var ui = row.gameObject.AddComponent<TitleSlotUI>();
            Set(ui, "playButton", play);
            Set(ui, "playLabel", playLabel);
            Set(ui, "deleteButton", del);
            Set(ui, "deleteLabel", delLabel);
            slots[i] = ui;
        }

        var fader = MakeFader(canvas);
        var menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
        Set(menu, "slots", slots);
        Set(menu, "fader", fader);

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/Title.unity");
    }

    // =====================================================================
    // Game 場景 (測試關卡)
    // =====================================================================

    static void BuildGameScene(Art art, Prefabs p)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int groundLayer = LayerMask.NameToLayer("Ground");

        // 切換場景後重新從磁碟載入，避免拿到已失效的參照
        p = new Prefabs
        {
            player = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Player.prefab"),
            savePoint = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SavePoint.prefab"),
            lever = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Lever.prefab"),
            platform = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/MovingPlatform.prefab"),
            door = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Door.prefab"),
            plate = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/PressurePlate.prefab"),
            breakable = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Breakable.prefab"),
            slot = AssetDatabase.LoadAssetAtPath<InventorySlotUI>($"{PrefabDir}/UI/InventorySlot.prefab"),
            mapNode = AssetDatabase.LoadAssetAtPath<MapNodeUI>($"{PrefabDir}/UI/MapNode.prefab"),
        };
        rocketItem = AssetDatabase.LoadAssetAtPath<RocketItem>($"{ItemDir}/Rocket.asset");
        batteryItem = AssetDatabase.LoadAssetAtPath<BatteryItem>($"{ItemDir}/Battery.asset");
        blackHoleItem = AssetDatabase.LoadAssetAtPath<BlackHoleGunItem>($"{ItemDir}/BlackHoleGun.asset");
        droneItem = AssetDatabase.LoadAssetAtPath<DroneItem>($"{ItemDir}/Drone.asset");
        art.circle = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/circle.png");

        // --- 光源 (URP 2D 需要 Global Light 才不會全黑) ---
        var light = new GameObject("Global Light 2D").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        var lso = new SerializedObject(light);
        lso.FindProperty("m_LightType").intValue = 4; // Global
        lso.ApplyModifiedPropertiesWithoutUndo();

        // --- Tilemap ---
        var grid = new GameObject("Grid").AddComponent<Grid>();
        var ground = MakeTilemap("Ground", grid.transform, groundLayer, 0);
        var hazards = MakeTilemap("Hazards", grid.transform, 0, 1);

        // 注意：一定要先畫格子再加碰撞體，否則 TilemapCollider2D 不會產生形狀 (玩家會穿過地板)
        PaintLevel(ground, hazards, art);

        var groundCol = ground.gameObject.AddComponent<TilemapCollider2D>();
        groundCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        ground.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        var composite = ground.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        groundCol.ProcessTilemapChanges();
        composite.GenerateGeometry();

        var hazardCol = hazards.gameObject.AddComponent<TilemapCollider2D>();
        hazardCol.isTrigger = true;
        hazardCol.ProcessTilemapChanges();
        hazards.gameObject.AddComponent<Hazard>();

        // --- 掉落死亡區 ---
        var kill = new GameObject("KillZone");
        kill.transform.position = new Vector3(62f, -6f, 0f); // 坑底 (y = -7) 往上 2 格
        var killCol = kill.AddComponent<BoxCollider2D>();
        killCol.isTrigger = true; killCol.size = new Vector2(180f, 2f);
        kill.AddComponent<Hazard>();

        // --- 出生點 / 玩家 ---
        var start = new GameObject("PlayerStart").transform;
        start.position = new Vector3(-8f, 1f, 0f);
        var player = ((GameObject)PrefabUtility.InstantiatePrefab(p.player)).GetComponent<PlayerController>();
        player.transform.position = start.position;

        // --- 存檔點 ---
        PlaceSavePoint(p, "起點營地", new Vector2(-3.5f, 0f));
        PlaceSavePoint(p, "高牆前", new Vector2(32.5f, 0f));      // 高牆要用火箭
        PlaceSavePoint(p, "壓力板前", new Vector2(48.5f, 0f));    // 電池壓板開門
        PlaceSavePoint(p, "第二區", new Vector2(84.5f, 0f));      // 黑洞打破牆
        PlaceSavePoint(p, "高架壓力板", new Vector2(106.5f, 0f)); // 把電池弄上高台

        // --- 機關 ---
        var doorA = PlaceDoor(p, 56, 0, 4);
        PlacePlate(p, new Vector2(51.5f, 0f), doorA);
        PlacePrefab(p.breakable, "Breakable", new Vector2(90.5f, 0.5f));
        PlacePrefab(p.breakable, "Breakable", new Vector2(90.5f, 1.5f));
        var doorB = PlaceDoor(p, 120, 0, 4);
        PlacePlate(p, new Vector2(113.5f, 3f), doorB);
        // 拉桿 + 橋：尖刺 (x = 125~126) 上方的橋一開始不存在，拉下拉桿才出現
        var bridge = PlacePrefab(p.door, "Bridge", new Vector2(125.5f, 0.5f)).GetComponent<Door>();
        bridge.transform.localScale = new Vector3(4f, 1f, 1f); // 橫放 = 橋，蓋住尖刺 (x = 123.5 ~ 127.5)
        Set(bridge, "startOpen", true);
        var lever = PlacePrefab(p.lever, "Lever", new Vector2(122.5f, 0f)).GetComponent<Lever>();
        Set(lever, "targets", new Activatable[] { bridge });

        // --- 地圖 B 延伸 ---
        PlaceSavePoint(p, "升降平台", new Vector2(138.5f, 0f));
        // 升降平台：凹槽 x = 142~144，頂面從 y = 0 升到 y = 6 (右邊高台)。
        // 站在平台上面向左用電池倉 → 電池剛好生成在 x = 141 的壓力板上 → 平台載人上升。
        // 平台最左格 (x = 142) 上方 y = 7 有低天花板 → 站在那格上升會被擠死 (測試用)。
        var lift = PlacePrefab(p.platform, "Lift", new Vector2(143.5f, -0.25f)).GetComponent<MovingPlatform>();
        lift.transform.Find("End").localPosition = new Vector3(0f, 6f, 0f);
        PlacePlate(p, new Vector2(141.5f, 0f), lift);
        // 無人機：x = 166 的牆只在 y = 10~11 有縫，無人機飛過去拉 x = 169 的拉桿 → 打開 x = 166 的門
        var doorC = PlaceDoor(p, 166, 0, 4);
        var droneLever = PlacePrefab(p.lever, "Lever (無人機)", new Vector2(169.5f, 0f)).GetComponent<Lever>();
        Set(droneLever, "targets", new Activatable[] { doorC });

        // --- 地圖區域 (鏡頭範圍) 與邊界轉場 ---
        var areaA = MakeArea("Area A", "地圖 A", -15, 61);
        MakeArea("Area B", "地圖 B", 80, 177);
        // 兩個邊界的 y 都放在 0，所以相對高度直接對應
        var edgeA = MakeEdge("Edge A → B", new Vector2(60.5f, 0f), new Vector2(-1.5f, 0f));
        var edgeB = MakeEdge("Edge B → A", new Vector2(80.5f, 0f), new Vector2(1.5f, 0f));
        Set(edgeA, "target", edgeB);
        Set(edgeB, "target", edgeA);

        // --- 鏡頭 ---
        var cam = MakeCamera();
        cam.gameObject.AddComponent<CinemachineBrain>();
        var vcam = new GameObject("CM Camera").AddComponent<CinemachineCamera>();
        vcam.Follow = player.transform;
        var lens = vcam.Lens; lens.OrthographicSize = 7f; vcam.Lens = lens;
        var composer = vcam.gameObject.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10f;
        vcam.gameObject.AddComponent<CinemachineConfiner2D>().BoundingShape2D = areaA.Bounds;
        vcam.gameObject.AddComponent<CameraAreaFollower>();

        // --- UI ---
        var canvas = MakeCanvas();
        MakeEventSystem();

        // 物品欄 (下方置中)
        var bar = NewUI("InventoryBar", canvas, new Vector2(800, 110));
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0, 30);
        var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var invUI = bar.gameObject.AddComponent<InventoryUI>();
        Set(invUI, "inventory", player.GetComponent<Inventory>());
        Set(invUI, "slotPrefab", p.slot);
        Set(invUI, "container", bar);

        // 道具狀態 (物品欄上方：提示文字 + 火箭藍條)
        var status = NewUI("ItemStatus", canvas, new Vector2(600, 90));
        status.anchorMin = status.anchorMax = new Vector2(0.5f, 0f);
        status.pivot = new Vector2(0.5f, 0f);
        status.anchoredPosition = new Vector2(0, 150);
        var barBg = NewImage("RocketBar", status, new Color(0f, 0f, 0f, 0.6f));
        barBg.rectTransform.anchorMin = barBg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        barBg.rectTransform.pivot = new Vector2(0.5f, 0f);
        barBg.rectTransform.sizeDelta = new Vector2(400, 16);
        barBg.raycastTarget = false;
        var barFill = NewImage("Fill", barBg.rectTransform, new Color(0.3f, 0.65f, 1f));
        barFill.rectTransform.anchorMin = Vector2.zero; barFill.rectTransform.anchorMax = Vector2.one;
        barFill.rectTransform.offsetMin = barFill.rectTransform.offsetMax = Vector2.zero;
        barFill.raycastTarget = false;
        var hint = NewText("Hint", status, "", 30, TextAnchor.MiddleCenter);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.sizeDelta = new Vector2(600, 50);
        hint.rectTransform.anchoredPosition = new Vector2(0, 28);
        hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        var statusUI = status.gameObject.AddComponent<ItemStatusUI>();
        Set(statusUI, "inventory", player.GetComponent<Inventory>());
        Set(statusUI, "rocket", player.GetComponent<PlayerRocket>());
        Set(statusUI, "hintGroup", hint.gameObject.AddComponent<CanvasGroup>());
        Set(statusUI, "hintLabel", hint);
        Set(statusUI, "barRoot", barBg.gameObject);
        Set(statusUI, "barFill", barFill.rectTransform);

        // 互動提示
        var promptHost = NewUI("InteractPrompt", canvas, Vector2.zero);
        var promptRoot = NewImage("Root", promptHost, new Color(0, 0, 0, 0.7f));
        promptRoot.rectTransform.sizeDelta = new Vector2(260, 56);
        var promptText = NewText("Text", promptRoot.rectTransform, "E 互動", 28, TextAnchor.MiddleCenter);
        Stretch(promptText.rectTransform, 0);
        var prompt = promptHost.gameObject.AddComponent<InteractPromptUI>();
        Set(prompt, "root", promptRoot.gameObject);
        Set(prompt, "label", promptText);

        // Toast
        var toast = NewImage("Toast", canvas, new Color(0, 0, 0, 0.7f));
        toast.rectTransform.anchorMin = toast.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        toast.rectTransform.pivot = new Vector2(0.5f, 1f);
        toast.rectTransform.sizeDelta = new Vector2(500, 70);
        toast.rectTransform.anchoredPosition = new Vector2(0, -40);
        toast.raycastTarget = false;
        var toastText = NewText("Text", toast.rectTransform, "", 32, TextAnchor.MiddleCenter);
        Stretch(toastText.rectTransform, 0);
        var toastUI = toast.gameObject.AddComponent<ToastUI>();
        Set(toastUI, "group", toast.gameObject.AddComponent<CanvasGroup>());
        Set(toastUI, "label", toastText);

        // 地圖
        var mapHost = NewUI("Map", canvas, Vector2.zero);
        Stretch(mapHost, 0);
        var mapPanel = NewImage("Panel", mapHost, new Color(0.05f, 0.07f, 0.1f, 0.92f));
        Stretch(mapPanel.rectTransform, 0);
        var mapTitle = NewText("Title", mapPanel.rectTransform, "地圖 (M 關閉)", 40, TextAnchor.MiddleCenter);
        mapTitle.rectTransform.anchorMin = mapTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        mapTitle.rectTransform.sizeDelta = new Vector2(600, 80);
        mapTitle.rectTransform.anchoredPosition = new Vector2(0, -70);
        var mapHint = NewText("Hint", mapPanel.rectTransform, "", 28, TextAnchor.MiddleCenter);
        mapHint.rectTransform.anchorMin = mapHint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        mapHint.rectTransform.sizeDelta = new Vector2(1200, 60);
        mapHint.rectTransform.anchoredPosition = new Vector2(0, 70);
        var nodeArea = NewUI("NodeArea", mapPanel.rectTransform, Vector2.zero);
        Stretch(nodeArea, 0);
        nodeArea.offsetMin = new Vector2(120, 120); nodeArea.offsetMax = new Vector2(-120, -160);
        var marker = NewImage("PlayerMarker", nodeArea, new Color(1f, 0.55f, 0.25f));
        marker.sprite = art.circle;
        marker.rectTransform.sizeDelta = new Vector2(18, 18);
        var mapUI = mapHost.gameObject.AddComponent<MapUI>();
        Set(mapUI, "panel", mapPanel.gameObject);
        Set(mapUI, "nodeArea", nodeArea);
        Set(mapUI, "nodePrefab", p.mapNode);
        Set(mapUI, "playerMarker", marker.rectTransform);
        Set(mapUI, "hintLabel", mapHint);

        var fader = MakeFader(canvas);

        // --- GameManager ---
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.player = player;
        gm.playerStart = start;
        gm.boundsTilemap = ground;
        gm.fader = fader;
        gm.vcam = vcam;
        gm.startingItems = new List<ItemDefinition> { rocketItem, batteryItem, blackHoleItem, droneItem };
        gmGO.AddComponent<DebugTools>();

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/Game.unity");
    }

    /// <summary>
    /// 測試關卡地形 (全部是封閉空間：有天花板和邊牆)。
    /// 地表高度 h 代表最上面的實心格在 y = h，站立面在 y = h + 1。天花板在 y = 14 ~ 16。
    /// 玩家跳躍高度約 2.4 格，3 格以上的牆要靠道具。
    /// </summary>
    static void PaintLevel(Tilemap ground, Tilemap hazards, Art art)
    {
        // 切換場景後重新從磁碟載入，避免拿到已被卸載的參照
        art.groundTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileDir}/Ground.asset");
        art.slopeRTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileDir}/SlopeRight.asset");
        art.slopeLTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileDir}/SlopeLeft.asset");
        art.spikeTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileDir}/Spike.asset");

        const int bottom = -8;
        void Fill(int x0, int x1, int y0, int y1)
        { for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) ground.SetTile(new Vector3Int(x, y, 0), art.groundTile); }
        void Range(int x0, int x1, int top) => Fill(x0, x1, bottom, top);
        void Ceiling(int x0, int x1) => Fill(x0, x1, 14, 16);
        void SlopeUp(int x0, int baseTop, int count)   // 往右上
        { for (int i = 0; i < count; i++) { Range(x0 + i, x0 + i, baseTop + i); ground.SetTile(new Vector3Int(x0 + i, baseTop + i + 1, 0), art.slopeRTile); } }
        void SlopeDown(int x0, int baseTop, int count) // 往右下
        { for (int i = 0; i < count; i++) { int top = baseTop - i; Range(x0 + i, x0 + i, top - 1); ground.SetTile(new Vector3Int(x0 + i, top, 0), art.slopeLTile); } }
        void Spike(int x, int y) => hazards.SetTile(new Vector3Int(x, y, 0), art.spikeTile);

        // ===== 地圖 A (x = -15 ~ 60) =====
        Range(-15, -12, 16);      // 左邊界牆
        Ceiling(-11, 60);
        Range(-11, 5, -1);        // 起點平地
        SlopeUp(6, -1, 3);        // 45 度上坡
        Range(9, 14, 2);          // 小高台
        SlopeDown(15, 2, 3);      // 45 度下坡
        Range(18, 21, -1);
        Range(22, 25, -8);        // 坑洞 (坑底有致死區)
        Range(26, 34, -1);
        Spike(28, 0); Spike(29, 0);
        Range(35, 46, 6);         // 7 格高的高牆 → 用火箭上去
        Range(47, 60, -1);
        Fill(56, 56, 4, 13);      // 門上方的牆 (門在 x = 56, y = 0~3)。右邊通到 x = 61 → 轉場到地圖 B

        // ===== 地圖 B (x = 80 ~ 176)，左邊界接地圖 A 的右邊界 =====
        Ceiling(80, 172);
        Range(80, 141, -1);
        Fill(90, 90, 2, 13);      // 可破壞牆上方 (x = 90, y = 0~1 是 Breakable)
        Range(112, 115, 2);       // 3 格高的平台，上面有壓力板 → 用黑洞把電池吸上去 / 火箭 + 電池
        Fill(120, 120, 4, 13);    // 門上方的牆 (門在 x = 120, y = 0~3)
        Spike(125, 0); Spike(126, 0);
        // --- 延伸：升降平台 + 無人機 ---
        Range(142, 144, -2);      // 升降平台的凹槽 (平台頂面與地面齊平)
        Range(145, 152, 5);       // 高台 (站立面 y = 6)
        Fill(142, 142, 7, 13);    // 升降平台最左格上方的低天花板 (擠壓測試)
        Range(153, 172, -1);
        Fill(166, 166, 4, 9);     // 門 (x = 166, y = 0~3) 上方的牆，y = 10~11 留縫給無人機
        Fill(166, 166, 12, 13);
        Range(173, 176, 16);      // 右邊界牆
    }

    /// <summary>建立一個區域：x 範圍 [xMin, xMax]，y 固定 -10 ~ 20。</summary>
    static MapArea MakeArea(string goName, string displayName, float xMin, float xMax)
    {
        var go = new GameObject(goName) { layer = 2 }; // Ignore Raycast
        var poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new[] { new Vector2(xMin, -10), new Vector2(xMax, -10), new Vector2(xMax, 20), new Vector2(xMin, 20) };
        var area = go.AddComponent<MapArea>();
        Set(area, "displayName", displayName);
        Set(area, "bounds", poly);
        return area;
    }

    static MapEdge MakeEdge(string name, Vector2 pos, Vector2 entryOffset)
    {
        var go = new GameObject(name) { layer = 2 };
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 30f);
        box.offset = new Vector2(0f, 5f);
        var edge = go.AddComponent<MapEdge>();
        Set(edge, "entryOffset", entryOffset);
        return edge;
    }

    static void PlaceSavePoint(Prefabs p, string name, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(p.savePoint);
        go.name = $"SavePoint ({name})";
        go.transform.position = pos;
        var sp = go.GetComponent<SavePoint>();
        Set(sp, "displayName", name);
        Set(sp, "id", "save_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
    }

    static GameObject PlacePrefab(GameObject prefab, string name, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    /// <summary>門佔據格子 x、y0 ~ y0 + height - 1。</summary>
    static Door PlaceDoor(Prefabs p, int x, int y0, int height)
    {
        var go = PlacePrefab(p.door, "Door", new Vector2(x + 0.5f, y0 + height * 0.5f));
        go.transform.localScale = new Vector3(1f, height, 1f);
        return go.GetComponent<Door>();
    }

    /// <summary>壓力板放在地面表面 surface 上。</summary>
    static void PlacePlate(Prefabs p, Vector2 surface, params Activatable[] targets)
    {
        var plate = PlacePrefab(p.plate, "PressurePlate", surface).GetComponent<PressurePlate>();
        Set(plate, "targets", targets);
    }

    static Tilemap MakeTilemap(string name, Transform parent, int layer, int order)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        var tm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = order;
        return tm;
    }

    // =====================================================================
    // 共用小工具
    // =====================================================================

    static Camera MakeCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0, 0, -10);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    static RectTransform MakeCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return (RectTransform)go.transform;
    }

    static void MakeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static ScreenFader MakeFader(RectTransform canvas)
    {
        var img = NewImage("ScreenFader", canvas, Color.black);
        Stretch(img.rectTransform, 0);
        img.gameObject.AddComponent<CanvasGroup>();
        return img.gameObject.AddComponent<ScreenFader>();
    }

    static RectTransform NewUI(string name, RectTransform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        if (parent != null) rt.SetParent(parent, false);
        rt.sizeDelta = size;
        return rt;
    }

    static Image NewImage(string name, RectTransform parent, Color color)
    {
        var img = NewUI(name, parent, new Vector2(100, 100)).gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text NewText(string name, RectTransform parent, string text, int size, TextAnchor align)
    {
        var t = NewUI(name, parent, new Vector2(200, 50)).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.supportRichText = true;
        return t;
    }

    static Button NewButton(string name, RectTransform parent, Vector2 size, out Text label, int fontSize)
    {
        var img = NewImage(name, parent, new Color(0.15f, 0.2f, 0.3f));
        img.rectTransform.sizeDelta = size;
        var btn = img.gameObject.AddComponent<Button>();
        label = NewText("Label", img.rectTransform, name, fontSize, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 0);
        return btn;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>設定 [SerializeField] private 欄位。</summary>
    static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"找不到欄位 {target.GetType().Name}.{field}"); return; }
        switch (value)
        {
            case Object o: prop.objectReferenceValue = o; break;
            case string s: prop.stringValue = s; break;
            case bool b: prop.boolValue = b; break;
            case int i: prop.intValue = i; break;
            case float f: prop.floatValue = f; break;
            case Vector2 v: prop.vector2Value = v; break;
            case Color c: prop.colorValue = c; break;
            case Object[] arr:
                prop.arraySize = arr.Length;
                for (int k = 0; k < arr.Length; k++) prop.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
