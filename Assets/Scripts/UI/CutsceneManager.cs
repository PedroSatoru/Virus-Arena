using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Gerencia exibição de telas de cutscene (imagem fullscreen + botão de continuar).
/// Singleton — permanece entre uso dentro da mesma cena.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Image cutsceneImage;
    public Button continueButton;
    public Text continueButtonText;

    private Action onContinue;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Exibe uma cutscene com a imagem do sprite informado.
    /// Ao pressionar continuar, executa o callback e fecha.
    /// </summary>
    public void Show(string spriteName, string buttonLabel, Action callback)
    {
        Sprite spr = LoadSprite(spriteName);
        if (spr != null && cutsceneImage != null)
            cutsceneImage.sprite = spr;

        if (continueButtonText != null)
            continueButtonText.text = buttonLabel;

        onContinue = callback;

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnContinueClicked()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        onContinue?.Invoke();
        onContinue = null;
    }

    Sprite LoadSprite(string name)
    {
        // Primeiro tenta Resources/Sprites/
        Sprite s = Resources.Load<Sprite>($"Sprites/{name}");
        if (s != null) return s;

        // Tenta direto em Resources/
        s = Resources.Load<Sprite>(name);
        if (s != null) return s;

#if UNITY_EDITOR
        // Fallback editor: carrega direto do path do asset
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
#endif
        return s;
    }
}
