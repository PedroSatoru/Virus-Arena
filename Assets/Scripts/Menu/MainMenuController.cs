using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controla o menu principal: título pulsante, botões Jogar/Créditos/Sair.
/// Deve ser colocado em um GameObject na cena MainMenu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Referências UI")]
    public Text titleText;
    public Button playButton;
    public Button creditsButton;
    public Button quitButton;
    public Text creditsText;
    public GameObject creditsPanel;

    private bool isPulsing = true;

    void Start()
    {
        // Configurar botões
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        // Iniciar pulsação do título
        if (titleText != null)
            StartCoroutine(PulseTitleCoroutine());
    }

    /// <summary>
    /// Efeito de pulsação suave no título.
    /// </summary>
    IEnumerator PulseTitleCoroutine()
    {
        RectTransform rt = titleText.GetComponent<RectTransform>();
        Vector3 originalScale = rt.localScale;
        float speed = 1.5f;
        float amplitude = 0.05f;

        while (isPulsing)
        {
            float scale = 1f + Mathf.Sin(Time.time * speed) * amplitude;
            rt.localScale = originalScale * scale;
            yield return null;
        }
    }

    void OnPlayClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    void OnCreditsClicked()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(!creditsPanel.activeSelf);
        }
    }

    void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        isPulsing = false;
    }
}
