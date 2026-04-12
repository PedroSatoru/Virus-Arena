using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;

/// <summary>
/// Script de Editor para montar automaticamente a GameScene (Fase 1 - Pulmão).
/// Acessível via menu: Virus Arena > Setup Game Scene
/// Cria TUDO: Câmera 2D (URP), Arena, Player, Inimigo, HUD, GameManager.
/// Você pode apagar toda a cena e apertar de novo para recriar tudo.
/// </summary>
public class SetupGameScene : Editor
{
    // Constantes da arena
    static float ARENA_WIDTH = 40f;
    static float ARENA_HEIGHT = 10f;
    static float ARENA_HALF_W = 20f;
    static float ARENA_HALF_H = 5f;
    static float WALL_THICKNESS = 0.5f;
    static float FLOOR_Y = -4.5f;
    static float CEILING_Y = 4.5f;

    static Sprite _cachedSprite;
    
    /// <summary>
    /// Retorna um sprite branco salvo como asset (persiste entre sessões).
    /// </summary>
    static Sprite GetPersistentWhiteSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;
        
        // Verificar se já existe
        string path = "Assets/Sprites/WhiteSquare.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
        {
            _cachedSprite = existing;
            return existing;
        }
        
        // Criar pasta
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        
        // Criar textura 4x4 branca e salvar como PNG
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        
        byte[] pngData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        
        // Configurar como sprite
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        
        _cachedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return _cachedSprite;
    }

    [MenuItem("Virus Arena/Setup Game Scene")]
    public static void SetupScene()
    {
        // Criar uma nova cena limpa
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Garantir sprite persistente
        Sprite whiteSprite = GetPersistentWhiteSprite();

        // ============ CÂMERA 2D com URP ============
        CreateCamera();

        // ============ ILUMINAÇÃO 2D (URP) ============
        CreateLighting();

        // ============ FUNDO ============
        CreateBackground(whiteSprite);

        // ============ ARENA ============
        GameObject arenaRoot = CreateArena(whiteSprite);

        // ============ PLAYER ============
        GameObject player = CreatePlayer(whiteSprite);

        // ============ PREFABS ============
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject playerBulletPrefab = CreatePlayerBulletPrefab(whiteSprite);
        GameObject enemyBulletPrefab = CreateEnemyBulletPrefab(whiteSprite);
        GameObject purpleBulletPrefab = CreatePurpleBulletPrefab(whiteSprite);
        GameObject antiCorpoPrefab = CreateAntiCorpoPrefab(whiteSprite, enemyBulletPrefab);
        GameObject playerShooterPrefab = CreatePlayerShooterPrefab(whiteSprite, purpleBulletPrefab);

        // ============ INIMIGO INICIAL ============
        CreateInitialEnemy(antiCorpoPrefab);

        // ============ HUD ============
        GameObject hudCanvas = CreateHUD();

        // ============ GAME MANAGER ============
        GameObject gmObj = CreateGameManager(antiCorpoPrefab, playerShooterPrefab, hudCanvas);

        // ============ CONECTAR REFERÊNCIAS ============
        PlayerShooting ps = player.GetComponent<PlayerShooting>();
        if (ps != null) ps.bulletPrefab = playerBulletPrefab;

        // Salvar cena
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        
        // Atualizar Build Settings
        UpdateBuildSettings();
        
        Debug.Log("✅ GameScene (Fase 1 - Pulmão) criada com sucesso!");
    }

    static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
    }

    // ============================================================
    // CÂMERA 2D
    // ============================================================
    static void CreateCamera()
    {
        GameObject cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        Camera c = cam.AddComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 5.5f;
        c.backgroundColor = new Color(0.15f, 0.02f, 0.05f);
        c.clearFlags = CameraClearFlags.SolidColor;
        c.nearClipPlane = -10f;
        c.farClipPlane = 100f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.AddComponent<AudioListener>();

        // Adicionar componente URP
        var camData = cam.AddComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Base;
    }

    // ============================================================
    // ILUMINAÇÃO 2D
    // ============================================================
    static void CreateLighting()
    {
        // Luz global 2D
        GameObject lightObj = new GameObject("Global Light 2D");
        var light2d = lightObj.AddComponent<Light2D>();
        light2d.lightType = Light2D.LightType.Global;
        light2d.intensity = 1f;
        light2d.color = new Color(1f, 0.9f, 0.9f); // Tom levemente rosado
    }

    // ============================================================
    // FUNDO
    // ============================================================
    static void CreateBackground(Sprite sprite)
    {
        // Fundo principal
        GameObject bg = CreateSprite("Background_Lung", sprite, new Color(0.65f, 0.25f, 0.3f),
            new Vector3(0, 0, 5), new Vector3(40f, 12f, 1f));
        bg.GetComponent<SpriteRenderer>().sortingOrder = -10;

        // Veias decorativas
        for (int i = 0; i < 6; i++)
        {
            float xPos = -7f + i * 2.5f;
            float yPos = Mathf.Sin(i * 1.2f) * 2f;
            float rot = -45f + i * 15f;
            GameObject vein = CreateSprite($"Vein_{i}", sprite, new Color(0.5f, 0.15f, 0.2f, 0.6f),
                new Vector3(xPos, yPos, 4), new Vector3(3f + i * 0.5f, 0.15f, 1f));
            vein.transform.rotation = Quaternion.Euler(0, 0, rot);
            vein.GetComponent<SpriteRenderer>().sortingOrder = -9;
            vein.transform.parent = bg.transform;
        }

        // Alvéolos decorativos
        for (int i = 0; i < 8; i++)
        {
            float xPos = -8f + i * 2.2f;
            float yPos = Mathf.Cos(i * 0.9f) * 3f;
            float size = 0.5f + (i % 3) * 0.4f;
            GameObject alveolus = CreateSprite($"Alveolus_{i}", sprite, new Color(0.8f, 0.4f, 0.5f, 0.3f),
                new Vector3(xPos, yPos, 3), new Vector3(size, size, 1f));
            alveolus.GetComponent<SpriteRenderer>().sortingOrder = -8;
            alveolus.transform.parent = bg.transform;
        }
    }

    // ============================================================
    // ARENA
    // ============================================================
    static GameObject CreateArena(Sprite sprite)
    {
        GameObject arenaRoot = new GameObject("Arena");
        ArenaManager am = arenaRoot.AddComponent<ArenaManager>();

        // Chão
        GameObject floor = CreateSprite("Arena_Floor", sprite, new Color(0.4f, 0.2f, 0.25f),
            new Vector3(0, FLOOR_Y, 0), new Vector3(ARENA_WIDTH, WALL_THICKNESS, 1f));
        floor.tag = "ArenaFloor";
        floor.layer = LayerMask.NameToLayer("Arena");
        floor.AddComponent<BoxCollider2D>();
        floor.GetComponent<SpriteRenderer>().sortingOrder = 0;
        floor.transform.parent = arenaRoot.transform;
        am.floorRenderer = floor.GetComponent<SpriteRenderer>();

        // Parede esquerda
        GameObject leftWall = CreateSprite("Arena_LeftWall", sprite, new Color(0.35f, 0.15f, 0.2f, 0.8f),
            new Vector3(-ARENA_HALF_W - WALL_THICKNESS / 2f, 0, 0), new Vector3(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS, 1f));
        leftWall.tag = "ArenaWall";
        leftWall.layer = LayerMask.NameToLayer("Arena");
        leftWall.AddComponent<BoxCollider2D>();
        leftWall.GetComponent<SpriteRenderer>().sortingOrder = 0;
        leftWall.transform.parent = arenaRoot.transform;
        am.leftWallRenderer = leftWall.GetComponent<SpriteRenderer>();

        // Parede direita
        GameObject rightWall = CreateSprite("Arena_RightWall", sprite, new Color(0.35f, 0.15f, 0.2f, 0.8f),
            new Vector3(ARENA_HALF_W + WALL_THICKNESS / 2f, 0, 0), new Vector3(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS, 1f));
        rightWall.tag = "ArenaWall";
        rightWall.layer = LayerMask.NameToLayer("Arena");
        rightWall.AddComponent<BoxCollider2D>();
        rightWall.GetComponent<SpriteRenderer>().sortingOrder = 0;
        rightWall.transform.parent = arenaRoot.transform;

        // Teto
        GameObject ceiling = CreateSprite("Arena_Ceiling", sprite, new Color(0.35f, 0.15f, 0.2f, 0.6f),
            new Vector3(0, CEILING_Y, 0), new Vector3(ARENA_WIDTH, WALL_THICKNESS, 1f));
        ceiling.tag = "ArenaCeiling";
        ceiling.layer = LayerMask.NameToLayer("Arena");
        ceiling.AddComponent<BoxCollider2D>();
        ceiling.GetComponent<SpriteRenderer>().sortingOrder = 0;
        ceiling.transform.parent = arenaRoot.transform;
        am.ceilingRenderer = ceiling.GetComponent<SpriteRenderer>();

        return arenaRoot;
    }

    // ============================================================
    // PLAYER
    // ============================================================
    static GameObject CreatePlayer(Sprite sprite)
    {
        GameObject player = CreateSprite("Player_GlobuloBranco", sprite, new Color(0.9f, 0.95f, 1f),
            new Vector3(0, -3.5f, 0), new Vector3(0.8f, 0.8f, 1f));
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        player.GetComponent<SpriteRenderer>().sortingOrder = 10;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f); // Cobre todo o sprite quadrado

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = 7f;
        pc.jumpForce = 12f;
        pc.minX = -8.5f;
        pc.maxX = 8.5f;
        pc.groundLayer = LayerMask.GetMask("Arena");

        PlayerShooting ps = player.AddComponent<PlayerShooting>();
        ps.bulletSpeed = 15f;
        ps.fireRate = 0.2f;

        PlayerHealth ph = player.AddComponent<PlayerHealth>();
        ph.maxHearts = 5;

        // Ground Check
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.parent = player.transform;
        groundCheck.transform.localPosition = new Vector3(0, -0.4f, 0);
        pc.groundCheck = groundCheck.transform;
        pc.groundCheckRadius = 0.15f;

        // Fire Point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.parent = player.transform;
        firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
        ps.firePoint = firePoint.transform;

        // Aim Line
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        string matPath = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matPath))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(lineMat, matPath + "/AimLineMaterial.mat");

        LineRenderer lr = player.AddComponent<LineRenderer>();
        lr.startWidth = 0.03f;
        lr.endWidth = 0.01f;
        lr.startColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        lr.endColor = new Color(0.5f, 0.8f, 1f, 0.1f);
        lr.positionCount = 2;
        lr.material = lineMat;
        lr.sortingOrder = 11;
        ps.aimLine = lr;

        // Núcleo visual
        GameObject nucleus = CreateSprite("Nucleus", sprite, new Color(0.6f, 0.65f, 0.9f, 0.8f),
            Vector3.zero, new Vector3(0.25f, 0.25f, 1f));
        nucleus.transform.parent = player.transform;
        nucleus.transform.localPosition = new Vector3(0.05f, -0.05f, 0);
        nucleus.GetComponent<SpriteRenderer>().sortingOrder = 11;

        return player;
    }

    // ============================================================
    // PREFAB PROJÉTIL DO PLAYER
    // ============================================================
    static GameObject CreatePlayerBulletPrefab(Sprite sprite)
    {
        GameObject bullet = CreateSprite("PlayerBullet", sprite, new Color(0.6f, 0.85f, 1f),
            Vector3.zero, new Vector3(0.2f, 0.2f, 1f));
        bullet.GetComponent<SpriteRenderer>().sortingOrder = 15;
        bullet.layer = LayerMask.NameToLayer("Projectile");

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.1f;

        Projectile proj = bullet.AddComponent<Projectile>();
        proj.bulletType = Projectile.BulletType.PlayerBullet;
        proj.damage = 1;
        proj.lifetime = 5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Prefabs/PlayerBullet.prefab");
        DestroyImmediate(bullet);
        return prefab;
    }

    // ============================================================
    // PREFAB PROJÉTIL DO INIMIGO
    // ============================================================
    static GameObject CreateEnemyBulletPrefab(Sprite sprite)
    {
        GameObject bullet = CreateSprite("EnemyBullet_AntiBody", sprite, new Color(1f, 0.9f, 0f),
            Vector3.zero, new Vector3(0.25f, 0.25f, 1f));
        bullet.GetComponent<SpriteRenderer>().sortingOrder = 15;
        bullet.layer = LayerMask.NameToLayer("Projectile");

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        Projectile proj = bullet.AddComponent<Projectile>();
        proj.bulletType = Projectile.BulletType.EnemyAntiBody;
        proj.damage = 1;
        proj.lifetime = 5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Prefabs/EnemyBullet_AntiBody.prefab");
        DestroyImmediate(bullet);
        return prefab;
    }

    // ============================================================
    // PREFAB DO INIMIGO
    // ============================================================
    static GameObject CreateEnemyPrefab(Sprite sprite, GameObject enemyBulletPrefab)
    {
        GameObject enemy = CreateSprite("Enemy_AntiCorpo", sprite, new Color(1f, 0.85f, 0f),
            Vector3.zero, new Vector3(0.7f, 0.7f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        EnemyShooter shooter = enemy.AddComponent<EnemyShooter>();
        shooter.bulletPrefab = enemyBulletPrefab;
        shooter.fireInterval = 2.5f;
        shooter.bulletSpeed = 4f; // Reduzido (era 8) a pedido do usuário
        shooter.currentPhase = 1;
        shooter.arenaMinX = -9f;
        shooter.arenaMaxX = 9f;
        shooter.arenaFloorY = FLOOR_Y;

        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.maxHP = 1;

        // Espinhos
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 spikePos = new Vector3(Mathf.Cos(rad) * 0.35f, Mathf.Sin(rad) * 0.35f, 0);
            GameObject spike = CreateSprite($"Spike_{i}", sprite, new Color(1f, 0.6f, 0f),
                Vector3.zero, new Vector3(0.12f, 0.2f, 1f));
            spike.transform.parent = enemy.transform;
            spike.transform.localPosition = spikePos;
            spike.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            spike.GetComponent<SpriteRenderer>().sortingOrder = 6;
        }

        // Olhos
        GameObject leftEye = CreateSprite("LeftEye", sprite, new Color(0.2f, 0f, 0f),
            Vector3.zero, new Vector3(0.15f, 0.06f, 1f));
        leftEye.transform.parent = enemy.transform;
        leftEye.transform.localPosition = new Vector3(-0.1f, 0.05f, -0.1f);
        leftEye.transform.localRotation = Quaternion.Euler(0, 0, 15f);
        leftEye.GetComponent<SpriteRenderer>().sortingOrder = 7;

        GameObject rightEye = CreateSprite("RightEye", sprite, new Color(0.2f, 0f, 0f),
            Vector3.zero, new Vector3(0.15f, 0.06f, 1f));
        rightEye.transform.parent = enemy.transform;
        rightEye.transform.localPosition = new Vector3(0.1f, 0.05f, -0.1f);
        rightEye.transform.localRotation = Quaternion.Euler(0, 0, -15f);
        rightEye.GetComponent<SpriteRenderer>().sortingOrder = 7;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemy_AntiCorpo.prefab");
        DestroyImmediate(enemy);
        return prefab;
    }

    // ============================================================
    // PREFAB PROJÉTIL ROXO (Anti-Player)
    // ============================================================
    static GameObject CreatePurpleBulletPrefab(Sprite sprite)
    {
        GameObject bullet = CreateSprite("EnemyBullet_AntiPlayer", sprite, new Color(0.6f, 0f, 0.8f),
            Vector3.zero, new Vector3(0.25f, 0.25f, 1f));
        bullet.GetComponent<SpriteRenderer>().sortingOrder = 15;
        bullet.layer = LayerMask.NameToLayer("Projectile");

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        Projectile proj = bullet.AddComponent<Projectile>();
        proj.bulletType = Projectile.BulletType.EnemyAntiPlayer;
        proj.damage = 1;
        proj.lifetime = 5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Prefabs/EnemyBullet_AntiPlayer.prefab");
        DestroyImmediate(bullet);
        return prefab;
    }

    // ============================================================
    // PREFAB DO INIMIGO ANTI-CORPO (Amarelo)
    // ============================================================
    static GameObject CreateAntiCorpoPrefab(Sprite sprite, GameObject enemyBulletPrefab)
    {
        GameObject enemy = CreateSprite("Enemy_AntiCorpo", sprite, new Color(1f, 0.85f, 0f),
            Vector3.zero, new Vector3(0.7f, 0.7f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        EnemyShooter shooter = enemy.AddComponent<EnemyShooter>();
        shooter.bulletPrefab = enemyBulletPrefab;
        shooter.fireInterval = 2.5f;
        shooter.bulletSpeed = 4f;
        shooter.currentPhase = 1;
        shooter.arenaMinX = -9f;
        shooter.arenaMaxX = 9f;
        shooter.arenaFloorY = FLOOR_Y;

        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.maxHP = 1;

        // Espinhos
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            float rad = angle * Mathf.Deg2Rad;
            GameObject spike = CreateSprite($"Spike_{i}", sprite, new Color(1f, 0.6f, 0f),
                Vector3.zero, new Vector3(0.12f, 0.2f, 1f));
            spike.transform.parent = enemy.transform;
            spike.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.35f, Mathf.Sin(rad) * 0.35f, 0);
            spike.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            spike.GetComponent<SpriteRenderer>().sortingOrder = 6;
        }

        // Olhos
        AddEyes(enemy, sprite, new Color(0.2f, 0f, 0f));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemy_AntiCorpo.prefab");
        DestroyImmediate(enemy);
        return prefab;
    }

    // ============================================================
    // PREFAB DO INIMIGO PLAYER SHOOTER (Roxo)
    // ============================================================
    static GameObject CreatePlayerShooterPrefab(Sprite sprite, GameObject purpleBulletPrefab)
    {
        GameObject enemy = CreateSprite("Enemy_PlayerShooter", sprite, new Color(0.6f, 0f, 0.8f),
            Vector3.zero, new Vector3(0.7f, 0.7f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        EnemyPlayerShooter pShooter = enemy.AddComponent<EnemyPlayerShooter>();
        pShooter.bulletPrefab = purpleBulletPrefab;
        pShooter.fireInterval = 3f;
        pShooter.bulletSpeed = 5f;
        pShooter.currentPhase = 1;
        pShooter.arenaMinX = -9f;
        pShooter.arenaMaxX = 9f;

        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.maxHP = 1;

        // Espinhos roxos
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            float rad = angle * Mathf.Deg2Rad;
            GameObject spike = CreateSprite($"Spike_{i}", sprite, new Color(0.4f, 0f, 0.6f),
                Vector3.zero, new Vector3(0.12f, 0.2f, 1f));
            spike.transform.parent = enemy.transform;
            spike.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.35f, Mathf.Sin(rad) * 0.35f, 0);
            spike.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            spike.GetComponent<SpriteRenderer>().sortingOrder = 6;
        }

        // Olhos vermelhos (mais agressivos)
        AddEyes(enemy, sprite, new Color(1f, 0f, 0.1f));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemy_PlayerShooter.prefab");
        DestroyImmediate(enemy);
        return prefab;
    }

    static void AddEyes(GameObject enemy, Sprite sprite, Color eyeColor)
    {
        GameObject leftEye = CreateSprite("LeftEye", sprite, eyeColor,
            Vector3.zero, new Vector3(0.15f, 0.06f, 1f));
        leftEye.transform.parent = enemy.transform;
        leftEye.transform.localPosition = new Vector3(-0.1f, 0.05f, -0.1f);
        leftEye.transform.localRotation = Quaternion.Euler(0, 0, 15f);
        leftEye.GetComponent<SpriteRenderer>().sortingOrder = 7;

        GameObject rightEye = CreateSprite("RightEye", sprite, eyeColor,
            Vector3.zero, new Vector3(0.15f, 0.06f, 1f));
        rightEye.transform.parent = enemy.transform;
        rightEye.transform.localPosition = new Vector3(0.1f, 0.05f, -0.1f);
        rightEye.transform.localRotation = Quaternion.Euler(0, 0, -15f);
        rightEye.GetComponent<SpriteRenderer>().sortingOrder = 7;
    }
    static void CreateInitialEnemy(GameObject enemyPrefab)
    {
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
        enemy.transform.position = new Vector3(3f, 2.5f, 0f);
        enemy.name = "Enemy_AntiCorpo_Initial";
    }

    // ============================================================
    // HUD
    // ============================================================
    static GameObject CreateHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        
        HUDManager hudMgr = canvasObj.AddComponent<HUDManager>();

        // EventSystem (necessário para botões do menu de pausa e game over)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // ==== BARRA DO CORPO (inferior, ponta a ponta) ====
        GameObject bodyBarBg = CreateUIImage("BodyHealthBar_BG", canvasObj.transform,
            new Color(0.15f, 0.05f, 0.05f, 0.9f));
        RectTransform bodyBgRT = bodyBarBg.GetComponent<RectTransform>();
        bodyBgRT.anchorMin = new Vector2(0, 0);
        bodyBgRT.anchorMax = new Vector2(1, 0);
        bodyBgRT.pivot = new Vector2(0.5f, 0);
        bodyBgRT.sizeDelta = new Vector2(-20, 30);
        bodyBgRT.anchoredPosition = new Vector2(0, 5);

        // Slider
        GameObject bodySliderObj = new GameObject("BodyHealthSlider");
        bodySliderObj.transform.SetParent(bodyBarBg.transform, false);
        RectTransform bodySliderRT = bodySliderObj.AddComponent<RectTransform>();
        bodySliderRT.anchorMin = Vector2.zero;
        bodySliderRT.anchorMax = Vector2.one;
        bodySliderRT.sizeDelta = new Vector2(-4, -4);
        Slider bodySlider = bodySliderObj.AddComponent<Slider>();
        bodySlider.transition = Selectable.Transition.None;

        GameObject bodyFillArea = new GameObject("Fill Area");
        bodyFillArea.transform.SetParent(bodySliderObj.transform, false);
        RectTransform bodyFillAreaRT = bodyFillArea.AddComponent<RectTransform>();
        bodyFillAreaRT.anchorMin = Vector2.zero;
        bodyFillAreaRT.anchorMax = Vector2.one;
        bodyFillAreaRT.sizeDelta = Vector2.zero;

        GameObject bodyFill = CreateUIImage("Fill", bodyFillArea.transform, new Color(0.7f, 0.1f, 0.1f));
        RectTransform bodyFillRT = bodyFill.GetComponent<RectTransform>();
        bodyFillRT.anchorMin = Vector2.zero;
        bodyFillRT.anchorMax = Vector2.one;
        bodyFillRT.sizeDelta = Vector2.zero;
        bodySlider.fillRect = bodyFillRT;
        bodySlider.maxValue = 1500;
        bodySlider.value = 1500;

        GameObject bodyLabel = CreateUIText("BodyLabel", bodyBarBg.transform, "CORPO: 1500/1500", 12, TextAnchor.MiddleCenter, Color.white);
        RectTransform bodyLabelRT = bodyLabel.GetComponent<RectTransform>();
        bodyLabelRT.anchorMin = Vector2.zero;
        bodyLabelRT.anchorMax = Vector2.one;
        bodyLabelRT.sizeDelta = Vector2.zero;

        hudMgr.bodyHealthSlider = bodySlider;
        hudMgr.bodyHealthFill = bodyFill.GetComponent<Image>();
        hudMgr.bodyHealthLabel = bodyLabel.GetComponent<Text>();

        // ==== PAINEL INFERIOR (acima da barra do corpo) ====
        GameObject bottomPanel = new GameObject("BottomPanel");
        bottomPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform bottomRT = bottomPanel.AddComponent<RectTransform>();
        bottomRT.anchorMin = new Vector2(0, 0);
        bottomRT.anchorMax = new Vector2(1, 0);
        bottomRT.pivot = new Vector2(0.5f, 0);
        bottomRT.sizeDelta = new Vector2(0, 60);
        bottomRT.anchoredPosition = new Vector2(0, 40);

        // --- CORAÇÕES (inferior esquerdo) ---
        Image[] heartImages = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject heart = CreateUIImage($"Heart_{i}", bottomPanel.transform, Color.red);
            RectTransform hrt = heart.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0.5f);
            hrt.anchorMax = new Vector2(0, 0.5f);
            hrt.anchoredPosition = new Vector2(30 + i * 35, 0);
            hrt.sizeDelta = new Vector2(28, 28);
            heartImages[i] = heart.GetComponent<Image>();

            GameObject ht = CreateUIText($"HS_{i}", heart.transform, "\u2665", 20, TextAnchor.MiddleCenter, Color.white);
            RectTransform htRT = ht.GetComponent<RectTransform>();
            htRT.anchorMin = Vector2.zero;
            htRT.anchorMax = Vector2.one;
            htRT.sizeDelta = Vector2.zero;
        }
        hudMgr.heartIcons = heartImages;

        // --- FASE + TIMER (centro) ---
        GameObject phaseText = CreateUIText("PhaseNameText", bottomPanel.transform, "FASE 1 \u2014 PULM\u00c3O", 18, TextAnchor.MiddleCenter, Color.white);
        RectTransform phaseRT = phaseText.GetComponent<RectTransform>();
        phaseRT.anchorMin = new Vector2(0.3f, 0.5f);
        phaseRT.anchorMax = new Vector2(0.7f, 1f);
        phaseRT.sizeDelta = Vector2.zero;
        hudMgr.phaseNameText = phaseText.GetComponent<Text>();

        GameObject timerText = CreateUIText("TimerText", bottomPanel.transform, "3:00", 24, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.8f));
        RectTransform timerRT = timerText.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0.3f, 0f);
        timerRT.anchorMax = new Vector2(0.7f, 0.55f);
        timerRT.sizeDelta = Vector2.zero;
        timerText.GetComponent<Text>().fontStyle = FontStyle.Bold;
        hudMgr.timerText = timerText.GetComponent<Text>();

        // --- BARRA DO ÓRGÃO (direito) ---
        GameObject organBarBg = CreateUIImage("OrganHealthBar_BG", bottomPanel.transform, new Color(0.15f, 0.1f, 0.05f, 0.9f));
        RectTransform organBgRT = organBarBg.GetComponent<RectTransform>();
        organBgRT.anchorMin = new Vector2(1, 0.5f);
        organBgRT.anchorMax = new Vector2(1, 0.5f);
        organBgRT.anchoredPosition = new Vector2(-120, 0);
        organBgRT.sizeDelta = new Vector2(200, 40);

        GameObject organSliderObj = new GameObject("OrganHealthSlider");
        organSliderObj.transform.SetParent(organBarBg.transform, false);
        RectTransform organSliderRT = organSliderObj.AddComponent<RectTransform>();
        organSliderRT.anchorMin = Vector2.zero;
        organSliderRT.anchorMax = Vector2.one;
        organSliderRT.sizeDelta = new Vector2(-4, -4);
        Slider organSlider = organSliderObj.AddComponent<Slider>();
        organSlider.transition = Selectable.Transition.None;

        GameObject organFillArea = new GameObject("Fill Area");
        organFillArea.transform.SetParent(organSliderObj.transform, false);
        RectTransform organFillAreaRT = organFillArea.AddComponent<RectTransform>();
        organFillAreaRT.anchorMin = Vector2.zero;
        organFillAreaRT.anchorMax = Vector2.one;
        organFillAreaRT.sizeDelta = Vector2.zero;

        GameObject organFill = CreateUIImage("Fill", organFillArea.transform, new Color(0.2f, 0.8f, 0.4f));
        RectTransform organFillRT = organFill.GetComponent<RectTransform>();
        organFillRT.anchorMin = Vector2.zero;
        organFillRT.anchorMax = Vector2.one;
        organFillRT.sizeDelta = Vector2.zero;
        organSlider.fillRect = organFillRT;
        organSlider.maxValue = 500;
        organSlider.value = 500;

        GameObject organLabel = CreateUIText("OrganLabel", organBarBg.transform, "\u00d3RG\u00c3O: 500/500", 12, TextAnchor.MiddleCenter, Color.white);
        RectTransform organLabelRT = organLabel.GetComponent<RectTransform>();
        organLabelRT.anchorMin = Vector2.zero;
        organLabelRT.anchorMax = Vector2.one;
        organLabelRT.sizeDelta = Vector2.zero;

        hudMgr.organHealthSlider = organSlider;
        hudMgr.organHealthFill = organFill.GetComponent<Image>();
        hudMgr.organHealthLabel = organLabel.GetComponent<Text>();

        // ==== PAUSA ====
        GameObject pausePanel = CreateUIImage("PausePanel", canvasObj.transform, new Color(0, 0, 0, 0.7f));
        RectTransform pauseRT = pausePanel.GetComponent<RectTransform>();
        pauseRT.anchorMin = Vector2.zero;
        pauseRT.anchorMax = Vector2.one;
        pauseRT.sizeDelta = Vector2.zero;

        CreateUIText("PauseTitle", pausePanel.transform, "PAUSADO", 36, TextAnchor.MiddleCenter, Color.white);
        CreateUIButton("ContinueBtn", pausePanel.transform, "CONTINUAR", new Vector2(0, -40), new Vector2(200, 40), new Color(0.2f, 0.6f, 0.3f));
        CreateUIButton("MenuBtn", pausePanel.transform, "MENU PRINCIPAL", new Vector2(0, -90), new Vector2(200, 40), new Color(0.6f, 0.2f, 0.2f));

        pausePanel.SetActive(false);
        hudMgr.pausePanel = pausePanel;

        // ==== GAME OVER ====
        GameObject goPanel = CreateUIImage("GameOverPanel", canvasObj.transform, new Color(0.1f, 0f, 0f, 0.85f));
        RectTransform goRT = goPanel.GetComponent<RectTransform>();
        goRT.anchorMin = Vector2.zero;
        goRT.anchorMax = Vector2.one;
        goRT.sizeDelta = Vector2.zero;

        CreateUIText("GOTitle", goPanel.transform, "GAME OVER", 48, TextAnchor.MiddleCenter, Color.red);
        GameObject goSub = CreateUIText("GOSub", goPanel.transform, "Fal\u00eancia Sist\u00eamica", 20, TextAnchor.MiddleCenter, new Color(1, 0.6f, 0.6f));
        goSub.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        CreateUIButton("RestartBtn", goPanel.transform, "TENTAR NOVAMENTE", new Vector2(0, -100), new Vector2(250, 40), new Color(0.6f, 0.2f, 0.2f));
        CreateUIButton("GoMenuBtn", goPanel.transform, "MENU PRINCIPAL", new Vector2(0, -150), new Vector2(250, 40), new Color(0.3f, 0.3f, 0.3f));

        goPanel.SetActive(false);

        return canvasObj;
    }

    // ============================================================
    // GAME MANAGER
    // ============================================================
    static GameObject CreateGameManager(GameObject antiCorpoPrefab, GameObject playerShooterPrefab, GameObject hudCanvas)
    {
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.totalTime = 180f;
        gm.bodyMaxHP = 1500f;
        gm.organMaxHP = 500f;
        gm.currentPhase = 1;
        gm.antiCorpoPrefab = antiCorpoPrefab;
        gm.playerShooterPrefab = playerShooterPrefab;
        gm.baseSpawnInterval = 4f;
        gm.minSpawnInterval = 1.5f;
        gm.maxTotalEnemies = 5;
        gm.maxPerType = 2;
        gm.arenaMinX = -9f;
        gm.arenaMaxX = 9f;
        gm.arenaMinY = 2f;
        gm.arenaMaxY = 4f;

        // Transform.Find funciona em objetos inativos (ao contrario de GameObject.Find)
        if (hudCanvas != null)
        {
            Transform goPanelTransform = hudCanvas.transform.Find("GameOverPanel");
            if (goPanelTransform != null)
                gm.gameOverPanel = goPanelTransform.gameObject;
        }

        return gmObj;
    }

    // ============================================================
    // UTILITÁRIOS
    // ============================================================
    static GameObject CreateSprite(string name, Sprite sprite, Color color, Vector3 pos, Vector3 scale)
    {
        GameObject obj = new GameObject(name);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        return obj;
    }

    static GameObject CreateUIImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    static GameObject CreateUIText(string name, Transform parent, string text,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return obj;
    }

    static GameObject CreateUIButton(string name, Transform parent, string label,
        Vector2 position, Vector2 size, Color bgColor)
    {
        GameObject btnObj = CreateUIImage(name, parent, bgColor);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor = bgColor * 0.7f;
        btn.colors = cb;

        CreateUIText(name + "_Txt", btnObj.transform, label, 16, TextAnchor.MiddleCenter, Color.white);
        return btnObj;
    }
}
