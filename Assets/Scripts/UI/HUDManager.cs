using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a HUD do jogo.
/// Layout:
///   - Inferior Esquerdo: 5 corações (vida do player)
///   - Inferior Central: Nome da fase + cronômetro
///   - Inferior Direito: Barra de vida do Órgão
///   - Inferior (ponta a ponta, ABAIXO de tudo): Barra de vida do Corpo total
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Corações")]
    public Image[] heartIcons; // Array de 5 imagens de coração
    public Color heartActiveColor = Color.red;
    public Color heartInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Fase e Timer")]
    public Text phaseNameText;
    public Text timerText;

    [Header("Barra de Vida do Órgão")]
    public Slider organHealthSlider;
    public Image organHealthFill;
    public Text organHealthLabel;

    [Header("Barra de Vida do Corpo (inferior total)")]
    public Slider bodyHealthSlider;
    public Image bodyHealthFill;
    public Text bodyHealthLabel;

    [Header("Menu de Pausa")]
    public GameObject pausePanel;

    private GameManager gameManager;
    private PlayerHealth playerHealth;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHearts;
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Inicializar HUD
        UpdateHearts(playerHealth != null ? playerHealth.currentHearts : 5);
    }

    void Update()
    {
        if (gameManager == null) return;

        // Atualizar timer
        UpdateTimer(gameManager.timeRemaining);

        // Atualizar barra de vida do órgão
        UpdateOrganHealth(gameManager.organCurrentHP, gameManager.organMaxHP);

        // Atualizar barra de vida do corpo
        UpdateBodyHealth(gameManager.bodyCurrentHP, gameManager.bodyMaxHP);

        // Toggle pausa
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Atualiza os ícones de coração baseado na vida atual.
    /// </summary>
    public void UpdateHearts(int currentHearts)
    {
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].color = (i < currentHearts) ? heartActiveColor : heartInactiveColor;
            }
        }
    }

    /// <summary>
    /// Atualiza o cronômetro regressivo.
    /// </summary>
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0}:{1:D2}", minutes, seconds);
    }

    /// <summary>
    /// Atualiza a barra de vida do órgão.
    /// </summary>
    public void UpdateOrganHealth(float current, float max)
    {
        if (organHealthSlider != null)
        {
            organHealthSlider.maxValue = max;
            organHealthSlider.value = current;
        }

        if (organHealthFill != null)
        {
            float ratio = current / max;
            organHealthFill.color = Color.Lerp(Color.red, new Color(0.2f, 0.8f, 0.4f), ratio);
        }

        if (organHealthLabel != null)
        {
            organHealthLabel.text = $"ÓRGÃO: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }

    /// <summary>
    /// Atualiza a barra de vida do corpo total (1500 HP).
    /// </summary>
    public void UpdateBodyHealth(float current, float max)
    {
        if (bodyHealthSlider != null)
        {
            bodyHealthSlider.maxValue = max;
            bodyHealthSlider.value = current;
        }

        if (bodyHealthFill != null)
        {
            float ratio = current / max;
            bodyHealthFill.color = Color.Lerp(new Color(0.5f, 0f, 0f), new Color(0.8f, 0.1f, 0.1f), ratio);
        }

        if (bodyHealthLabel != null)
        {
            bodyHealthLabel.text = $"CORPO: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }

    public void TogglePause()
    {
        if (pausePanel == null) return;

        bool isPaused = !pausePanel.activeSelf;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
        Time.timeScale = 1f;
    }
}
