using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Script de Editor para montar a cena do Menu Principal.
/// Menu: Virus Arena > Setup Menu Scene
/// Cria câmera 2D, iluminação, fundo arterial e UI completa.
/// Pode-se apagar tudo e rodar novamente para recriar.
/// </summary>
public class SetupMenuScene : Editor
{
    [MenuItem("Virus Arena/Setup Menu Scene")]
    public static void SetupScene()
    {
        // Cena limpa
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ============ CÂMERA 2D ============
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.12f, 0.02f, 0.04f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = -10f;
        cam.farClipPlane = 100f;
        cameraObj.transform.position = new Vector3(0, 0, -10);
        cameraObj.AddComponent<AudioListener>();
        
        // URP Camera Data
        var camData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Base;

        // ============ ILUMINAÇÃO 2D ============
        GameObject lightObj = new GameObject("Global Light 2D");
        var light2d = lightObj.AddComponent<Light2D>();
        light2d.lightType = Light2D.LightType.Global;
        light2d.intensity = 1f;
        light2d.color = new Color(1f, 0.85f, 0.85f);

        // ============ FUNDO VISUAL ============
        CreateMenuBackground();

        // ============ CANVAS UI ============
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem (necessário para botões)
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        MainMenuController menuCtrl = canvasObj.AddComponent<MainMenuController>();

        // ============ TÍTULO ============
        GameObject titleObj = CreateText("TitleText", canvasObj.transform, "VIRUS\nARENA",
            72, TextAnchor.MiddleCenter, new Color(0.9f, 0.15f, 0.15f));
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.2f, 0.45f);
        titleRT.anchorMax = new Vector2(0.8f, 0.9f);
        titleRT.sizeDelta = Vector2.zero;
        titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
        menuCtrl.titleText = titleObj.GetComponent<Text>();

        // Subtítulo
        GameObject subtitleObj = CreateText("SubtitleText", canvasObj.transform, "\u2014 DEFENDA O ORGANISMO \u2014",
            18, TextAnchor.MiddleCenter, new Color(0.7f, 0.3f, 0.3f, 0.8f));
        RectTransform subRT = subtitleObj.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.2f, 0.40f);
        subRT.anchorMax = new Vector2(0.8f, 0.48f);
        subRT.sizeDelta = Vector2.zero;

        // ============ BOTÃO JOGAR ============
        GameObject playBtn = CreateButton("PlayButton", canvasObj.transform, "JOGAR",
            new Vector2(0.5f, 0.28f), new Vector2(280, 55), new Color(0.7f, 0.12f, 0.12f), 24);
        menuCtrl.playButton = playBtn.GetComponent<Button>();

        // ============ BOTÃO CRÉDITOS ============
        GameObject creditsBtn = CreateButton("CreditsButton", canvasObj.transform, "TUTORIAL / CR\u00c9DITOS",
            new Vector2(0.25f, 0.13f), new Vector2(240, 40), new Color(0.3f, 0.15f, 0.15f), 16);
        menuCtrl.creditsButton = creditsBtn.GetComponent<Button>();

        // ============ BOTÃO SAIR ============
        GameObject quitBtn = CreateButton("QuitButton", canvasObj.transform, "SAIR",
            new Vector2(0.75f, 0.13f), new Vector2(180, 40), new Color(0.3f, 0.15f, 0.15f), 16);
        menuCtrl.quitButton = quitBtn.GetComponent<Button>();

        // ============ PAINEL CRÉDITOS ============
        GameObject creditsPanel = new GameObject("CreditsPanel");
        creditsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform cpRT = creditsPanel.AddComponent<RectTransform>();
        cpRT.anchorMin = Vector2.zero;
        cpRT.anchorMax = Vector2.one;
        cpRT.sizeDelta = Vector2.zero;
        Image cpImg = creditsPanel.AddComponent<Image>();
        cpImg.color = new Color(0, 0, 0, 0.85f);

        CreateText("CreditsTitle", creditsPanel.transform, "CR\u00c9DITOS", 32, TextAnchor.MiddleCenter, Color.white)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
        CreateText("CreditsContent", creditsPanel.transform,
            "Virus Arena\n\nDesenvolvido para a disciplina de\nDesenvolvimento de Jogos\n\nCi\u00eancias da Computa\u00e7\u00e3o\n\n\u2014 Clique para fechar \u2014",
            18, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

        creditsPanel.SetActive(false);
        menuCtrl.creditsPanel = creditsPanel;

        // Versão
        GameObject versionText = CreateText("VersionText", canvasObj.transform, "Prot\u00f3tipo v0.1",
            12, TextAnchor.LowerRight, new Color(0.5f, 0.3f, 0.3f, 0.5f));
        RectTransform verRT = versionText.GetComponent<RectTransform>();
        verRT.anchorMin = new Vector2(0.8f, 0f);
        verRT.anchorMax = new Vector2(1f, 0.05f);
        verRT.sizeDelta = Vector2.zero;

        // ============ SALVAR ============
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        UpdateBuildSettings();
        Debug.Log("\u2705 MainMenu criada com sucesso! Pressione Play para testar.");
    }

    static void CreateMenuBackground()
    {
        // Garantir sprite persistente
        Sprite whiteSprite = GetPersistentWhiteSprite();

        GameObject bg = new GameObject("Background");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 0.05f, 0.08f);
        bg.transform.position = new Vector3(0, 0, 10);
        bg.transform.localScale = new Vector3(20, 12, 1);
        sr.sortingOrder = -10;

        // Veias
        for (int i = 0; i < 8; i++)
        {
            GameObject vein = new GameObject($"Vein_{i}");
            vein.transform.parent = bg.transform;
            SpriteRenderer vsr = vein.AddComponent<SpriteRenderer>();
            vsr.sprite = whiteSprite;
            vsr.color = new Color(0.3f, 0.08f, 0.12f, 0.5f);
            vsr.sortingOrder = -9;
            vein.transform.position = new Vector3(-8f + i * 2.3f, Mathf.Sin(i * 1.1f) * 3f, 9);
            vein.transform.localScale = new Vector3(3f + i * 0.7f, 0.1f, 1f);
            vein.transform.rotation = Quaternion.Euler(0, 0, -60f + i * 17f);
        }

        // Células decorativas
        for (int i = 0; i < 12; i++)
        {
            GameObject cell = new GameObject($"Cell_{i}");
            cell.transform.parent = bg.transform;
            SpriteRenderer csr = cell.AddComponent<SpriteRenderer>();
            csr.sprite = whiteSprite;
            csr.color = new Color(0.3f, 0.1f, 0.15f, 0.2f);
            csr.sortingOrder = -8;
            float size = 0.3f + (i % 4) * 0.2f;
            cell.transform.position = new Vector3(-9f + i * 1.6f, Mathf.Cos(i * 0.8f) * 4f, 8);
            cell.transform.localScale = new Vector3(size, size, 1f);
        }
    }

    static Sprite GetPersistentWhiteSprite()
    {
        string path = "Assets/Sprites/WhiteSquare.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("\u2705 Build Settings: MainMenu (0), GameScene (1)");
    }

    // ============================================================
    // UTILITÁRIOS UI
    // ============================================================
    static GameObject CreateText(string name, Transform parent, string text,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.4f);
        rt.anchorMax = new Vector2(0.9f, 0.6f);
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

    static GameObject CreateButton(string name, Transform parent, string label,
        Vector2 anchorPos, Vector2 size, Color bgColor, int fontSize)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor = bgColor * 0.7f;
        btn.colors = cb;

        GameObject textObj = CreateText(name + "_Text", btnObj.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        return btnObj;
    }
}
