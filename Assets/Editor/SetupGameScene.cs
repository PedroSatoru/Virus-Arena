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

    /// <summary>
    /// Carrega um sprite específico da pasta Assets/Sprites.
    /// </summary>
    static Sprite GetSprite(string name)
    {
        string path = $"Assets/Sprites/{name}.png";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            // Fallback para o branco se não encontrar
            return GetPersistentWhiteSprite();
        }
        return s;
    }

    [MenuItem("Virus Arena/Setup Game Scene - FASE 1")]
    public static void SetupPhase1()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildPhase(1, "Assets/Scenes/GameScene.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene (Fase 1) criada com sucesso!");
    }

    [MenuItem("Virus Arena/Setup Game Scene - FASE 2")]
    public static void SetupPhase2()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildPhase(2, "Assets/Scenes/GameScene_Ph2.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene_Ph2 (Fase 2) criada com sucesso!");
    }

    [MenuItem("Virus Arena/Setup Game Scene - FASE 3")]
    public static void SetupPhase3()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildPhase(3, "Assets/Scenes/GameScene_Ph3.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene_Ph3 (Fase 3) criada com sucesso!");
    }

    // ============================================================
    // MODO INFINITO
    // ============================================================

    [MenuItem("Virus Arena/Infinite Mode/Setup Infinite Select Scene")]
    public static void SetupInfiniteSelect()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildInfiniteSelectScene(whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ InfiniteSelectScene criada com sucesso!");
    }

    [MenuItem("Virus Arena/Infinite Mode/Setup Infinite Scene - FASE 1")]
    public static void SetupInfinitePhase1()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildInfinitePhase(1, "Assets/Scenes/GameScene_Inf1.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene_Inf1 (Infinito Fase 1) criada com sucesso!");
    }

    [MenuItem("Virus Arena/Infinite Mode/Setup Infinite Scene - FASE 2")]
    public static void SetupInfinitePhase2()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildInfinitePhase(2, "Assets/Scenes/GameScene_Inf2.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene_Inf2 (Infinito Fase 2) criada com sucesso!");
    }

    [MenuItem("Virus Arena/Infinite Mode/Setup Infinite Scene - FASE 3")]
    public static void SetupInfinitePhase3()
    {
        Sprite whiteSprite = GetPersistentWhiteSprite();
        BuildInfinitePhase(3, "Assets/Scenes/GameScene_Inf3.unity", whiteSprite);
        UpdateBuildSettings();
        Debug.Log("✅ GameScene_Inf3 (Infinito Fase 3) criada com sucesso!");
    }

    // ============================================================
    // BUILD — FASE INFINITA (reutiliza a arena normal, troca GameManager)
    // ============================================================
    static void BuildInfinitePhase(int phase, string scenePath, Sprite whiteSprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateLighting();

        string bgName = phase switch {
            1 => "pulmao",
            2 => "coraçao",
            _ => "Cerebro"
        };
        CreateBackground(bgName);
        CreateArena(whiteSprite, phase);
        GameObject player = CreatePlayer(whiteSprite);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject playerBulletPrefab  = CreatePlayerBulletPrefab(whiteSprite);
        GameObject enemyBulletPrefab   = CreateEnemyBulletPrefab(whiteSprite);
        GameObject purpleBulletPrefab  = CreatePurpleBulletPrefab(whiteSprite);
        GameObject antiCorpoPrefab     = CreateAntiCorpoPrefab(whiteSprite, enemyBulletPrefab);
        GameObject playerShooterPrefab = CreatePlayerShooterPrefab(whiteSprite, purpleBulletPrefab);
        GameObject kamikazePrefab      = CreateKamikazePrefab(whiteSprite);

        // Boss disponível em todas as fases infinitas (aparece na fase 3 a cada 5 min)
        GameObject bossPrefab = CreateBossPrefab(whiteSprite, enemyBulletPrefab, purpleBulletPrefab);

        CreateInitialEnemy(antiCorpoPrefab);

        GameObject hudCanvas = CreateInfiniteHUD(phase);
        CreateInfiniteGameManager(antiCorpoPrefab, playerShooterPrefab, kamikazePrefab, bossPrefab, hudCanvas, phase);

        PlayerShooting ps = player.GetComponent<PlayerShooting>();
        if (ps != null) ps.bulletPrefab = playerBulletPrefab;

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ============================================================
    // BUILD — CENA DE SELEÇÃO DO MODO INFINITO
    // ============================================================
    static void BuildInfiniteSelectScene(Sprite whiteSprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Câmera básica
        CreateCamera();
        CreateLighting();

        // Canvas principal
        GameObject canvasObj = new GameObject("InfiniteSelect_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        InfiniteSelectMenuController ctrl = canvasObj.AddComponent<InfiniteSelectMenuController>();

        // EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject evSys = new GameObject("EventSystem");
            evSys.AddComponent<EventSystem>();
            evSys.AddComponent<StandaloneInputModule>();
        }

        // Fundo escuro
        GameObject bg = CreateUIImage("Background", canvasObj.transform, new Color(0.05f, 0f, 0.08f, 1f));
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Título
        GameObject title = CreateUIText("Title", canvasObj.transform, "MODO INFINITO", 48, TextAnchor.MiddleCenter, new Color(0.9f, 0.3f, 1f));
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.8f);
        titleRT.anchorMax = new Vector2(0.9f, 1f);
        titleRT.sizeDelta = Vector2.zero;
        title.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Subtítulo
        GameObject sub = CreateUIText("Subtitle", canvasObj.transform, "Selecione uma fase para jogar em modo sem fim", 22, TextAnchor.MiddleCenter, new Color(0.8f, 0.7f, 0.9f));
        RectTransform subRT = sub.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.73f);
        subRT.anchorMax = new Vector2(0.9f, 0.83f);
        subRT.sizeDelta = Vector2.zero;

        // ─── 3 Caixas de seleção ───
        string[] phaseNames     = { "FASE 1\nPULMÃO",      "FASE 2\nCORAÇÃO",      "FASE 3\nCÉREBRO"       };
        Color[]  phaseColors    = { new Color(0.55f, 0.1f, 0.2f), new Color(0.1f, 0.15f, 0.55f), new Color(0.15f, 0.35f, 0.1f) };
        Color[]  phaseBtnColors = { new Color(0.75f, 0.15f, 0.3f), new Color(0.15f, 0.2f, 0.75f), new Color(0.2f, 0.5f, 0.15f) };
        float[]  anchorXMins    = { 0.04f,  0.37f, 0.70f };
        float[]  anchorXMaxes   = { 0.34f,  0.67f, 0.97f };

        Button[] phaseButtons     = new Button[3];
        Text[]   phaseRecordTexts = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            // Caixa principal
            GameObject box = CreateUIImage($"PhaseBox_{i+1}", canvasObj.transform, phaseColors[i]);
            RectTransform boxRT = box.GetComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(anchorXMins[i], 0.22f);
            boxRT.anchorMax = new Vector2(anchorXMaxes[i], 0.72f);
            boxRT.sizeDelta = Vector2.zero;

            // Nome da fase
            GameObject nameText = CreateUIText($"PhaseName_{i+1}", box.transform, phaseNames[i], 28, TextAnchor.UpperCenter, Color.white);
            RectTransform nameRT = nameText.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.05f, 0.6f);
            nameRT.anchorMax = new Vector2(0.95f, 0.95f);
            nameRT.sizeDelta = Vector2.zero;
            nameText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Record
            GameObject recordText = CreateUIText($"PhaseRecord_{i+1}", box.transform, "RECORDE: --:--", 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 0.6f));
            RectTransform recordRT = recordText.GetComponent<RectTransform>();
            recordRT.anchorMin = new Vector2(0.05f, 0.4f);
            recordRT.anchorMax = new Vector2(0.95f, 0.62f);
            recordRT.sizeDelta = Vector2.zero;
            phaseRecordTexts[i] = recordText.GetComponent<Text>();

            // Botão JOGAR
            GameObject btnObj = CreateUIButton($"Phase{i+1}Btn", box.transform, "▶  JOGAR", new Vector2(0, -40), new Vector2(220, 55), phaseBtnColors[i]);
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.05f);
            btnRT.anchorMax = new Vector2(0.5f, 0.05f);
            btnRT.anchoredPosition = new Vector2(0, 30);
            btnRT.sizeDelta = new Vector2(220, 55);
            phaseButtons[i] = btnObj.GetComponent<Button>();
        }

        // Botão Voltar
        GameObject backBtn = CreateUIButton("BackBtn", canvasObj.transform, "← MENU PRINCIPAL", new Vector2(0, 0), new Vector2(250, 50), new Color(0.3f, 0.3f, 0.35f));
        RectTransform backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f);
        backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0, 60);
        backRT.sizeDelta = new Vector2(250, 50);

        // Conectar referências ao controller
        ctrl.phase1Button = phaseButtons[0];
        ctrl.phase2Button = phaseButtons[1];
        ctrl.phase3Button = phaseButtons[2];
        ctrl.phase1RecordText = phaseRecordTexts[0];
        ctrl.phase2RecordText = phaseRecordTexts[1];
        ctrl.phase3RecordText = phaseRecordTexts[2];
        ctrl.backButton = backBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/InfiniteSelectScene.unity");
    }

    // ============================================================
    // HUD DO MODO INFINITO
    // ============================================================
    /// <summary>
    /// Cria a HUD para as cenas de Modo Infinito.
    /// Igual à HUD normal, exceto:
    ///   - Timer exibe tempo crescente (0:00 → ∞)
    ///   - Painel de Game Over mostra tempo + recorde
    ///   - Painel de PowerUp in-game (aparece a cada 3 min)
    ///   - Texto de fase indica "INFINITO — FASE X"
    /// </summary>
    static GameObject CreateInfiniteHUD(int phase)
    {
        // Reutiliza a HUD base
        GameObject canvasObj = CreateHUD(phase);

        // Corrigir nome da fase para indicar modo infinito
        HUDManager hudMgr = canvasObj.GetComponent<HUDManager>();
        if (hudMgr != null && hudMgr.phaseNameText != null)
        {
            string infLabel = phase switch
            {
                1 => "∞ INFINITO — PULMÃO",
                2 => "∞ INFINITO — CORAÇÃO",
                _ => "∞ INFINITO — CÉREBRO",
            };
            hudMgr.phaseNameText.text = infLabel;
        }

        // Timer começa em 0:00 (crescente)
        if (hudMgr != null && hudMgr.timerText != null)
            hudMgr.timerText.text = "0:00";

        // ==== PAINEL DE GAME OVER INFINITO ====
        // Substitui o GoPanel normal por um com campos de tempo e recorde
        Transform goOld = canvasObj.transform.Find("GameOverPanel");
        if (goOld != null) Object.DestroyImmediate(goOld.gameObject);

        GameObject goPanel = CreateUIImage("InfiniteGameOverPanel", canvasObj.transform, new Color(0.05f, 0f, 0.1f, 0.9f));
        RectTransform goRT = goPanel.GetComponent<RectTransform>();
        goRT.anchorMin = Vector2.zero;
        goRT.anchorMax = Vector2.one;
        goRT.sizeDelta = Vector2.zero;

        CreateUIText("GOTitle", goPanel.transform, "FIM DA RUN", 48, TextAnchor.MiddleCenter, new Color(0.9f, 0.3f, 1f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);

        GameObject timeTextObj = CreateUIText("TimeText", goPanel.transform, "TEMPO: 0:00", 30, TextAnchor.MiddleCenter, Color.white);
        timeTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

        GameObject recordTextObj = CreateUIText("RecordText", goPanel.transform, "RECORDE: --:--", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 0.5f));
        recordTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

        GameObject newRecordTextObj = CreateUIText("NewRecordText", goPanel.transform, "✦ NOVO RECORDE! ✦", 26, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.4f));
        newRecordTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        newRecordTextObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
        newRecordTextObj.SetActive(false); // Mostrado apenas se bater recorde

        CreateUIButton("RestartBtn",  goPanel.transform, "TENTAR NOVAMENTE",  new Vector2(0, -90),  new Vector2(280, 50), new Color(0.4f, 0.1f, 0.6f));
        CreateUIButton("GoMenuBtn",   goPanel.transform, "SELECIONAR FASE",   new Vector2(0, -150), new Vector2(280, 50), new Color(0.3f, 0.3f, 0.35f));

        goPanel.SetActive(false);

        return canvasObj;
    }

    // ============================================================
    // GAME MANAGER DO MODO INFINITO
    // ============================================================
    static GameObject CreateInfiniteGameManager(
        GameObject antiCorpoPrefab, GameObject playerShooterPrefab,
        GameObject kamikazePrefab, GameObject bossPrefab,
        GameObject hudCanvas, int phase)
    {
        GameObject gmObj = new GameObject("InfiniteGameManager");
        InfiniteGameManager igm = gmObj.AddComponent<InfiniteGameManager>();

        igm.currentPhase     = phase;
        igm.bodyMaxHP        = 1500f;
        igm.organMaxHP       = 500f;
        igm.antiCorpoPrefab  = antiCorpoPrefab;
        igm.playerShooterPrefab = playerShooterPrefab;
        igm.kamikazePrefab   = kamikazePrefab;
        igm.bossPrefab       = bossPrefab;
        igm.baseSpawnInterval = 2.5f;
        igm.minSpawnInterval  = 1f;
        igm.maxTotalEnemies   = 8;
        igm.maxPerType        = 3;
        igm.arenaMinX = -9f;
        igm.arenaMaxX =  9f;
        igm.arenaMinY = phase >= 3 ? -2f : 2f;
        igm.arenaMaxY = 4f;

        if (hudCanvas != null)
        {
            // Painel de Game Over infinito
            Transform goPanelT = hudCanvas.transform.Find("InfiniteGameOverPanel");
            if (goPanelT != null)
            {
                igm.infiniteGameOverPanel = goPanelT.gameObject;
                igm.infiniteTimeText   = goPanelT.Find("TimeText")?.GetComponent<Text>();
                igm.infiniteRecordText = goPanelT.Find("RecordText")?.GetComponent<Text>();
                igm.infiniteNewRecordText = goPanelT.Find("NewRecordText")?.GetComponent<Text>();
            }

            // Painel de PowerUp (reutiliza o mesmo da HUD normal)
            Transform pUpT = hudCanvas.transform.Find("PowerUpPanel");
            if (pUpT != null) igm.powerUpPanel = pUpT.gameObject;

            // Barra de vida do boss (fase 3)
            if (phase == 3)
            {
                Transform bossBarT = hudCanvas.transform.Find("BossHealthPanel");
                if (bossBarT != null) igm.bossHealthPanel = bossBarT.gameObject;
            }
        }

        return gmObj;
    }

    static void BuildPhase(int phase, string scenePath, Sprite whiteSprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ============ SETUP BASE ============
        CreateCamera();
        CreateLighting();

        string bgName = phase switch {
            1 => "pulmao",
            2 => "coraçao",
            _ => "Cerebro"
        };
        CreateBackground(bgName);
        GameObject arenaRoot = CreateArena(whiteSprite, phase);
        GameObject player = CreatePlayer(whiteSprite);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject playerBulletPrefab = CreatePlayerBulletPrefab(whiteSprite);
        GameObject enemyBulletPrefab = CreateEnemyBulletPrefab(whiteSprite);
        GameObject purpleBulletPrefab = CreatePurpleBulletPrefab(whiteSprite);
        GameObject antiCorpoPrefab = CreateAntiCorpoPrefab(whiteSprite, enemyBulletPrefab);
        GameObject playerShooterPrefab = CreatePlayerShooterPrefab(whiteSprite, purpleBulletPrefab);
        GameObject kamikazePrefab = CreateKamikazePrefab(whiteSprite);

        // Boss prefab (apenas fase 3)
        GameObject bossPrefab = null;
        if (phase == 3)
        {
            bossPrefab = CreateBossPrefab(whiteSprite, enemyBulletPrefab, purpleBulletPrefab);
        }

        CreateInitialEnemy(antiCorpoPrefab);

        GameObject hudCanvas = CreateHUD(phase);
        CreateCutsceneCanvas();

        GameObject gmObj = CreateGameManager(antiCorpoPrefab, playerShooterPrefab, kamikazePrefab, bossPrefab, hudCanvas, phase);

        PlayerShooting ps = player.GetComponent<PlayerShooting>();
        if (ps != null) ps.bulletPrefab = playerBulletPrefab;

        // Salvar cena
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",           true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",           true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene_Ph2.unity",       true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene_Ph3.unity",       true),
            // Modo Infinito
            new EditorBuildSettingsScene("Assets/Scenes/InfiniteSelectScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene_Inf1.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene_Inf2.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene_Inf3.unity",      true),
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
    static void CreateBackground(string spriteName)
    {
        Sprite bgSprite = GetSprite(spriteName);
        
        Vector3 pos;
        Vector3 scale;

        if (spriteName == "Cerebro")
        {
            // Medidas específicas para a Fase 3 (Cérebro)
            pos = new Vector3(-0.2796f, -0.2115f, 5f);
            scale = new Vector3(0.63674f, 0.7504317f, 1f);
        }
        else
        {
            // Medidas para Fase 1 e 2
            pos = new Vector3(0.0219f, -0.1971f, 5f);
            scale = new Vector3(0.9937807f, 0.4689629f, 1f);
        }

        GameObject bg = CreateSprite("Background_Artwork", bgSprite, Color.white, pos, scale);
        bg.GetComponent<SpriteRenderer>().sortingOrder = -10;
    }

    // ============================================================
    // ARENA
    // ============================================================
    static GameObject CreateArena(Sprite sprite, int phase)
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
            new Vector3(-9.35000038f, -0.409999996f, 0f), new Vector3(0.5f, 10.5f, 1f));
        leftWall.tag = "ArenaWall";
        leftWall.layer = LayerMask.NameToLayer("Arena");
        leftWall.AddComponent<BoxCollider2D>();
        leftWall.GetComponent<SpriteRenderer>().sortingOrder = 0;
        leftWall.transform.parent = arenaRoot.transform;
        am.leftWallRenderer = leftWall.GetComponent<SpriteRenderer>();

        // Parede direita
        GameObject rightWall = CreateSprite("Arena_RightWall", sprite, new Color(0.35f, 0.15f, 0.2f, 0.8f),
            new Vector3(9.28999996f, -0.0399999991f, 0f), new Vector3(0.5f, 9.89414978f, 1f));
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

        if (phase >= 2)
        {
            // Plataforma esquerda (lateral)
            GameObject leftPlat = CreateSprite("Platform_Left", sprite, new Color(0.4f, 0.2f, 0.25f),
                new Vector3(-7.5f, -2.3f, 0), new Vector3(2f, 0.25f, 1f));
            leftPlat.tag = "Arena";
            leftPlat.layer = LayerMask.NameToLayer("Arena");
            leftPlat.AddComponent<BoxCollider2D>();
            leftPlat.GetComponent<SpriteRenderer>().sortingOrder = 0;
            leftPlat.transform.parent = arenaRoot.transform;

            // Plataforma direita (lateral)
            GameObject rightPlat = CreateSprite("Platform_Right", sprite, new Color(0.4f, 0.2f, 0.25f),
                new Vector3(7.5f, -2.3f, 0), new Vector3(2f, 0.25f, 1f));
            rightPlat.tag = "Arena";
            rightPlat.layer = LayerMask.NameToLayer("Arena");
            rightPlat.AddComponent<BoxCollider2D>();
            rightPlat.GetComponent<SpriteRenderer>().sortingOrder = 0;
            rightPlat.transform.parent = arenaRoot.transform;
        }

        if (phase >= 3)
        {
            // Plataformas centrais móveis (sobem e descem dessincronizadas)
            Color movingPlatColor = new Color(0.35f, 0.18f, 0.3f);

            // Altura base = -0.3f, Amplitude = 2f => Min = -2.3f (altura das plataformas fixas), Max = 1.7f
            // Comprimento aumentado de 1.5 para 2.8 para possibilitar pulo entre elas
            // Plataforma central esquerda
            GameObject centerLeft = CreateSprite("Platform_CenterLeft", sprite, movingPlatColor,
                new Vector3(-3.5f, -0.3f, 0), new Vector3(2.0f, 0.25f, 1f));
            centerLeft.tag = "Arena";
            centerLeft.layer = LayerMask.NameToLayer("Arena");
            centerLeft.AddComponent<BoxCollider2D>();
            centerLeft.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            centerLeft.GetComponent<SpriteRenderer>().sortingOrder = 0;
            centerLeft.transform.parent = arenaRoot.transform;
            MovingPlatform mpLeft = centerLeft.AddComponent<MovingPlatform>();
            mpLeft.speed = 1.2f;
            mpLeft.amplitude = 2f;
            mpLeft.phaseOffset = 0f;

            // Plataforma central direita
            GameObject centerRight = CreateSprite("Platform_CenterRight", sprite, movingPlatColor,
                new Vector3(3.5f, -0.3f, 0), new Vector3(2.0f, 0.25f, 1f));
            centerRight.tag = "Arena";
            centerRight.layer = LayerMask.NameToLayer("Arena");
            centerRight.AddComponent<BoxCollider2D>();
            centerRight.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            centerRight.GetComponent<SpriteRenderer>().sortingOrder = 0;
            centerRight.transform.parent = arenaRoot.transform;
            MovingPlatform mpRight = centerRight.AddComponent<MovingPlatform>();
            mpRight.speed = 1.5f;
            mpRight.amplitude = 2f;
            mpRight.phaseOffset = 3.14f; // Dessincronizada
        }

        return arenaRoot;
    }

    // ============================================================
    // PLAYER
    // ============================================================
    static GameObject CreatePlayer(Sprite whiteSprite)
    {
        Sprite playerSprite = GetSprite("Player");
        GameObject player = CreateSprite("Player_GlobuloBranco", playerSprite, Color.white,
            new Vector3(0, -3.5f, 0), new Vector3(0.2f, 0.2f, 1f));
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        player.GetComponent<SpriteRenderer>().sortingOrder = 10;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(7.0f, 7.0f); // Tamanho perfeito solicitado pelo usuário
        col.offset = Vector2.zero;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = 7f;
        pc.jumpForce = 13.2f; // Salto levemente reduzido, ajustado para as novas plataformas
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
    static GameObject CreateEnemyPrefab(Sprite whiteSprite, GameObject enemyBulletPrefab)
    {
        Sprite enemySprite = GetSprite("AntiCorpo");
        GameObject enemy = CreateSprite("Enemy_AntiCorpo", enemySprite, Color.white,
            Vector3.zero, new Vector3(0.175f, 0.175f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(8.0f, 8.0f);
        col.offset = Vector2.zero;

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
    static GameObject CreateAntiCorpoPrefab(Sprite whiteSprite, GameObject enemyBulletPrefab)
    {
        Sprite enemySprite = GetSprite("AntiCorpo");
        GameObject enemy = CreateSprite("Enemy_AntiCorpo", enemySprite, Color.white,
            Vector3.zero, new Vector3(0.175f, 0.175f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(8.0f, 8.0f);
        col.offset = Vector2.zero;

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

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemy_AntiCorpo.prefab");
        DestroyImmediate(enemy);
        return prefab;
    }

    // ============================================================
    // PREFAB DO INIMIGO KAMIKAZE
    // ============================================================
    static GameObject CreateKamikazePrefab(Sprite whiteSprite)
    {
        Sprite kSprite = GetSprite("Kamikaze");
        GameObject enemy = CreateSprite("Enemy_Kamikaze", kSprite, Color.white,
            Vector3.zero, new Vector3(0.15f, 0.15f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(5.0f, 5.0f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.maxHP = 2; // Pode aguentar 2 tiros por exemplo, ou 1. Vamos deixar 1.
        health.maxHP = 1;

        EnemyKamikaze kamikaze = enemy.AddComponent<EnemyKamikaze>();
        kamikaze.baseMoveSpeed = 3.5f;
        kamikaze.explosionRadius = 1.5f;
        kamikaze.playerDamage = 1;
        kamikaze.bodyDamage = 15f;
        kamikaze.arenaMinX = -9f;
        kamikaze.arenaMaxX = 9f;
        kamikaze.arenaMinY = -4.5f;
        kamikaze.arenaMaxY = 4.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemy_Kamikaze.prefab");
        DestroyImmediate(enemy);
        return prefab;
    }

    // ============================================================
    // PREFAB DO INIMIGO PLAYER SHOOTER (Roxo)
    // ============================================================
    static GameObject CreatePlayerShooterPrefab(Sprite whiteSprite, GameObject purpleBulletPrefab)
    {
        Sprite enemySprite = GetSprite("PlayerShooter");
        GameObject enemy = CreateSprite("Enemy_PlayerShooter", enemySprite, Color.white,
            Vector3.zero, new Vector3(0.175f, 0.175f, 1f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 5;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(8.0f, 8.0f);
        col.offset = Vector2.zero;

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

    // ============================================================
    // PREFAB DO BOSS (Fase 3 - Vírus Final)
    // ============================================================
    static GameObject CreateBossPrefab(Sprite whiteSprite, GameObject enemyBulletPrefab, GameObject purpleBulletPrefab)
    {
        Sprite bossSprite = GetSprite("Boss");
        GameObject boss = CreateSprite("Boss_Virus", bossSprite, Color.white,
            Vector3.zero, new Vector3(0.25f, 0.25f, 1f));
        boss.tag = "Enemy";
        boss.layer = LayerMask.NameToLayer("Enemy");
        boss.GetComponent<SpriteRenderer>().sortingOrder = 8;

        BoxCollider2D col = boss.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3.5f, 3.5f);

        Rigidbody2D rb = boss.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.maxHP = 100;
        health.isInvulnerable = true; // Começa invulnerável

        BossController bc = boss.AddComponent<BossController>();
        bc.antiBodyBulletPrefab = enemyBulletPrefab;
        bc.antiPlayerBulletPrefab = purpleBulletPrefab;
        bc.bulletSpeed = 7f;
        bc.bodyDamagePerAttack = 50f;
        bc.telegraphDuration = 1.5f;
        bc.telegraphHighWallDuration = 2.5f;
        bc.telegraphCeilingDuration = 3f;
        bc.telegraphFloorDuration = 0.35f;
        bc.vulnerableDuration = 2.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(boss, "Assets/Prefabs/Boss_Virus.prefab");
        DestroyImmediate(boss);
        return prefab;
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
    static GameObject CreateHUD(int phase)
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
        string phaseStr;
        if (phase == 1) phaseStr = "FASE 1 \u2014 PULM\u00c3O";
        else if (phase == 2) phaseStr = "FASE 2 \u2014 CORA\u00c7\u00c3O";
        else phaseStr = "FASE 3 \u2014 C\u00c9REBRO";
        GameObject phaseText = CreateUIText("PhaseNameText", bottomPanel.transform, phaseStr, 18, TextAnchor.MiddleCenter, Color.white);
        RectTransform phaseRT = phaseText.GetComponent<RectTransform>();
        phaseRT.anchorMin = new Vector2(0.3f, 0.5f);
        phaseRT.anchorMax = new Vector2(0.7f, 1f);
        phaseRT.sizeDelta = Vector2.zero;
        hudMgr.phaseNameText = phaseText.GetComponent<Text>();

        string startTimerStr = phase == 1 ? "0:10" : "3:00";
        GameObject timerText = CreateUIText("TimerText", bottomPanel.transform, startTimerStr, 24, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.8f));
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

        // ==== POWER UP UI (Sempre gera mas só usa na fase 1) ====
        GameObject pUpPanel = CreateUIImage("PowerUpPanel", canvasObj.transform, new Color(1f, 1f, 1f, 0.95f)); // Tela clara tipo o sketch
        RectTransform pUpRT = pUpPanel.GetComponent<RectTransform>();
        pUpRT.anchorMin = Vector2.zero;
        pUpRT.anchorMax = Vector2.one;
        pUpRT.sizeDelta = Vector2.zero;

        // Fundo simulado do painel quadrado
        GameObject pBox = CreateUIImage("PowerUpBox", pUpPanel.transform, new Color(0.92f, 0.92f, 0.95f, 1f));
        RectTransform pBoxRT = pBox.GetComponent<RectTransform>();
        pBoxRT.anchorMin = new Vector2(0.5f, 0.5f);
        pBoxRT.anchorMax = new Vector2(0.5f, 0.5f);
        pBoxRT.sizeDelta = new Vector2(400, 450);
        
        CreateUIText("Title", pBox.transform, "DNA UPGRADES", 28, TextAnchor.MiddleCenter, new Color(0.2f, 0.2f, 0.2f)).GetComponent<Text>().fontStyle = FontStyle.Bold;
        pBox.transform.Find("Title").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);

        CreateUIButton("TripleBtn", pBox.transform, "1. TRIPLE SHOT", new Vector2(0, 70), new Vector2(300, 60), Color.white);
        CreateUIButton("SpeedBtn", pBox.transform, "2. SPEED BOOST", new Vector2(0, -10), new Vector2(300, 60), new Color(0.85f, 0.85f, 0.85f));
        CreateUIButton("LifeBtn", pBox.transform, "3. EXTRA <3 + 30% LIFE", new Vector2(0, -90), new Vector2(300, 60), Color.white);

        // Ajusta as cores das bordas e texto
        foreach(Transform child in pBox.transform)
        {
            if (child.name.EndsWith("Btn"))
            {
                child.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
                var txt = child.Find(child.name + "_Txt").GetComponent<Text>();
                txt.color = Color.black;
            }
        }

        pUpPanel.SetActive(false);

        // ==== BARRA DE VIDA DO BOSS (topo, oculta inicialmente) ====
        if (phase == 3)
        {
            GameObject bossBarPanel = new GameObject("BossHealthPanel");
            bossBarPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform bossBarPanelRT = bossBarPanel.AddComponent<RectTransform>();
            bossBarPanelRT.anchorMin = new Vector2(0.15f, 1f);
            bossBarPanelRT.anchorMax = new Vector2(0.85f, 1f);
            bossBarPanelRT.pivot = new Vector2(0.5f, 1f);
            bossBarPanelRT.sizeDelta = new Vector2(0, 40);
            bossBarPanelRT.anchoredPosition = new Vector2(0, -5);

            // Fundo da barra
            Image bossBarBgImg = bossBarPanel.AddComponent<Image>();
            bossBarBgImg.color = new Color(0.1f, 0f, 0.1f, 0.9f);

            // Slider
            GameObject bossSliderObj = new GameObject("BossHealthSlider");
            bossSliderObj.transform.SetParent(bossBarPanel.transform, false);
            RectTransform bossSliderRT = bossSliderObj.AddComponent<RectTransform>();
            bossSliderRT.anchorMin = Vector2.zero;
            bossSliderRT.anchorMax = Vector2.one;
            bossSliderRT.sizeDelta = new Vector2(-8, -8);
            Slider bossSlider = bossSliderObj.AddComponent<Slider>();
            bossSlider.transition = Selectable.Transition.None;

            GameObject bossFillArea = new GameObject("Fill Area");
            bossFillArea.transform.SetParent(bossSliderObj.transform, false);
            RectTransform bossFillAreaRT = bossFillArea.AddComponent<RectTransform>();
            bossFillAreaRT.anchorMin = Vector2.zero;
            bossFillAreaRT.anchorMax = Vector2.one;
            bossFillAreaRT.sizeDelta = Vector2.zero;

            GameObject bossFill = CreateUIImage("Fill", bossFillArea.transform, new Color(0.5f, 0f, 0.7f));
            RectTransform bossFillRT = bossFill.GetComponent<RectTransform>();
            bossFillRT.anchorMin = Vector2.zero;
            bossFillRT.anchorMax = Vector2.one;
            bossFillRT.sizeDelta = Vector2.zero;
            bossSlider.fillRect = bossFillRT;
            bossSlider.maxValue = 100;
            bossSlider.value = 100;

            GameObject bossLabel = CreateUIText("BossLabel", bossBarPanel.transform, "BOSS: 100/100", 14, TextAnchor.MiddleCenter, Color.white);
            RectTransform bossLabelRT = bossLabel.GetComponent<RectTransform>();
            bossLabelRT.anchorMin = Vector2.zero;
            bossLabelRT.anchorMax = Vector2.one;
            bossLabelRT.sizeDelta = Vector2.zero;

            bossBarPanel.SetActive(false);
        }

        // ==== PAINEL DE VITÓRIA ====
        if (phase == 3)
        {
            GameObject vicPanel = CreateUIImage("VictoryPanel", canvasObj.transform, new Color(0f, 0.05f, 0.1f, 0.9f));
            RectTransform vicRT = vicPanel.GetComponent<RectTransform>();
            vicRT.anchorMin = Vector2.zero;
            vicRT.anchorMax = Vector2.one;
            vicRT.sizeDelta = Vector2.zero;

            CreateUIText("VicTitle", vicPanel.transform, "VIT\u00d3RIA!", 48, TextAnchor.MiddleCenter, new Color(0.2f, 1f, 0.4f));
            GameObject vicSub = CreateUIText("VicSub", vicPanel.transform, "V\u00edrus derrotado! O corpo est\u00e1 salvo.", 20, TextAnchor.MiddleCenter, new Color(0.7f, 1f, 0.8f));
            vicSub.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            CreateUIButton("VictoryMenuBtn", vicPanel.transform, "MENU PRINCIPAL", new Vector2(0, -120), new Vector2(250, 40), new Color(0.2f, 0.5f, 0.3f));

            vicPanel.SetActive(false);
        }

        return canvasObj;
    }

    // ============================================================
    // GAME MANAGER
    // ============================================================
    static GameObject CreateGameManager(GameObject antiCorpoPrefab, GameObject playerShooterPrefab, GameObject kamikazePrefab, GameObject bossPrefab, GameObject hudCanvas, int phase)
    {
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.totalTime = (phase == 1 || phase == 3) ? 10f : 180f; // Fases 1 e 3 com 10 seg para teste
        gm.bodyMaxHP = 1500f;
        gm.organMaxHP = 500f;
        gm.currentPhase = phase;
        gm.antiCorpoPrefab = antiCorpoPrefab;
        gm.playerShooterPrefab = playerShooterPrefab;
        gm.kamikazePrefab = kamikazePrefab;
        gm.bossPrefab = bossPrefab;
        gm.baseSpawnInterval = 2.5f;
        gm.minSpawnInterval = 1f;
        gm.maxTotalEnemies = 8;
        gm.maxPerType = 3;
        gm.arenaMinX = -9f;
        gm.arenaMaxX = 9f;
        // Na fase 3 os inimigos podem descer mais baixo (plataformas móveis no meio)
        gm.arenaMinY = phase >= 3 ? -2f : 2f;
        gm.arenaMaxY = 4f;

        // Transform.Find funciona em objetos inativos (ao contrario de GameObject.Find)
        if (hudCanvas != null)
        {
            Transform goPanelTransform = hudCanvas.transform.Find("GameOverPanel");
            if (goPanelTransform != null)
                gm.gameOverPanel = goPanelTransform.gameObject;

            Transform pUpPanelTransform = hudCanvas.transform.Find("PowerUpPanel");
            if (pUpPanelTransform != null)
                gm.powerUpPanel = pUpPanelTransform.gameObject;

            // Boss UI e Vitória (apenas fase 3)
            if (phase == 3)
            {
                Transform bossPanel = hudCanvas.transform.Find("BossHealthPanel");
                if (bossPanel != null)
                    gm.bossHealthPanel = bossPanel.gameObject;

                Transform vicPanel = hudCanvas.transform.Find("VictoryPanel");
                if (vicPanel != null)
                    gm.victoryPanel = vicPanel.gameObject;
            }
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

    // ============================================================
    // CUTSCENE CANVAS
    // ============================================================
    static void CreateCutsceneCanvas()
    {
        GameObject canvasObj = new GameObject("CutsceneCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Acima de tudo

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        CutsceneManager cm = canvasObj.AddComponent<CutsceneManager>();

        // Painel de fundo preto fullscreen
        GameObject panel = new GameObject("CutscenePanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.95f);

        // Imagem da cutscene (fullscreen)
        GameObject imgObj = new GameObject("CutsceneImage");
        imgObj.transform.SetParent(panel.transform, false);
        RectTransform imgRT = imgObj.AddComponent<RectTransform>();
        imgRT.anchorMin = new Vector2(0.05f, 0.1f);
        imgRT.anchorMax = new Vector2(0.95f, 0.95f);
        imgRT.sizeDelta = Vector2.zero;
        Image cutsceneImg = imgObj.AddComponent<Image>();
        cutsceneImg.preserveAspect = true;
        cm.cutsceneImage = cutsceneImg;

        // Botão continuar (parte inferior)
        GameObject btnObj = CreateUIButton("CutsceneContinueBtn", panel.transform,
            "CONTINUAR ▶", new Vector2(0, 0), new Vector2(300, 55), new Color(0.15f, 0.15f, 0.2f));
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0, 35);
        btnRT.sizeDelta = new Vector2(300, 55);
        cm.continueButton = btnObj.GetComponent<Button>();

        // Texto do botão
        Transform btnTxt = btnObj.transform.Find("CutsceneContinueBtn_Txt");
        if (btnTxt != null) cm.continueButtonText = btnTxt.GetComponent<Text>();

        cm.panel = panel;
        panel.SetActive(false);
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
