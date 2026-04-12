using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// GameManager Singleton - Gerencia o estado global do jogo.
/// Sistema de spawn com limite POR TIPO de inimigo:
///   - Max 5 inimigos totais em tela
///   - Max 2 de cada tipo (AntiCorpo amarelo, PlayerShooter roxo)
/// Se órgão morre: pode continuar para próxima fase. Se corpo morre: Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Timer")]
    public float totalTime = 180f; // 3 minutos
    public float timeRemaining;

    [Header("Vida do Corpo")]
    public float bodyMaxHP = 1500f;
    public float bodyCurrentHP;

    [Header("Vida do Órgão")]
    public float organMaxHP = 500f;
    public float organCurrentHP;

    [Header("Fase Atual")]
    public int currentPhase = 1; // 1=Pulmão, 2=Coração, 3=Cérebro
    public float damageMultiplier = 1f;

    [Header("Prefabs dos Inimigos")]
    public GameObject antiCorpoPrefab;       // Atirador Anti-Corpo (Amarelo)
    public GameObject playerShooterPrefab;   // Atirador Anti-Player (Roxo)

    [Header("Spawn de Inimigos")]
    public float baseSpawnInterval = 4f;
    public float minSpawnInterval = 1.5f;
    public int maxTotalEnemies = 5;          // Máximo total em tela
    public int maxPerType = 2;               // Máximo de cada tipo

    [Header("Arena")]
    public float arenaMinX = -9f;
    public float arenaMaxX = 9f;
    public float arenaMinY = 2f;
    public float arenaMaxY = 4f;

    [Header("Estado")]
    public bool isGameOver = false;
    public bool isOrganDead = false;
    public bool isPaused = false;

    [Header("UI Game Over")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;  // Texto de motivo (opcional)

    // Spawn state
    private float spawnTimer;
    private int nextEnemyTypeIndex = 0; // Alterna entre tipos de inimigo

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        timeRemaining = totalTime;
        bodyCurrentHP = bodyMaxHP;
        organCurrentHP = organMaxHP;
        damageMultiplier = currentPhase;
        spawnTimer = 2f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Auto-fiação dos botões do Game Over (via Transform.Find - funciona em objetos inativos)
        if (gameOverPanel != null)
        {
            WireButton(gameOverPanel.transform, "RestartBtn", RestartPhase);
            WireButton(gameOverPanel.transform, "GoMenuBtn", ReturnToMenu);
        }

        // Auto-fiação do painel de pausa (buscar pelo HUDManager em runtime)
        HUDManager hud = FindFirstObjectByType<HUDManager>();
        if (hud != null && hud.pausePanel != null)
        {
            WireButton(hud.pausePanel.transform, "ContinueBtn", () => hud.TogglePause());
            WireButton(hud.pausePanel.transform, "MenuBtn", ReturnToMenu);
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnPlayerDeath += OnPlayerDied;
    }

    void Update()
    {
        if (isGameOver || isPaused) return;

        // Timer
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            OnPhaseCompleted();
            return;
        }

        // Spawn
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnEnemy();
            float progress = 1f - (timeRemaining / totalTime);
            spawnTimer = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);
        }
    }

    /// <summary>
    /// Tenta fazer spawn de um inimigo respeitando os limites por tipo e total.
    /// Alterna entre tipos para balancear.
    /// </summary>
    void TrySpawnEnemy()
    {
        // Contar ativos
        int antiCorpoCount = FindObjectsByType<EnemyShooter>(FindObjectsSortMode.None).Length;
        int playerShooterCount = FindObjectsByType<EnemyPlayerShooter>(FindObjectsSortMode.None).Length;
        int totalCount = antiCorpoCount + playerShooterCount;

        if (totalCount >= maxTotalEnemies) return;

        Vector3 spawnPos = GetRandomSpawnPos();

        // Determinar qual tipo pode spawnar
        bool canSpawnAntiCorpo = antiCorpoPrefab != null && antiCorpoCount < maxPerType;
        bool canSpawnPlayerShooter = playerShooterPrefab != null && playerShooterCount < maxPerType;

        if (!canSpawnAntiCorpo && !canSpawnPlayerShooter) return;

        // Alternar entre tipos disponíveis
        GameObject chosenPrefab = null;
        if (canSpawnAntiCorpo && canSpawnPlayerShooter)
        {
            // Alternar: se nextEnemyTypeIndex é par → AntiCorpo, ímpar → PlayerShooter
            chosenPrefab = (nextEnemyTypeIndex % 2 == 0) ? antiCorpoPrefab : playerShooterPrefab;
        }
        else if (canSpawnAntiCorpo)
        {
            chosenPrefab = antiCorpoPrefab;
        }
        else
        {
            chosenPrefab = playerShooterPrefab;
        }

        nextEnemyTypeIndex++;

        GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);

        // Configurar fase no EnemyShooter (AntiCorpo)
        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
        if (shooter != null)
        {
            shooter.currentPhase = currentPhase;
            shooter.arenaMinX = arenaMinX;
            shooter.arenaMaxX = arenaMaxX;
        }

        // Configurar fase no EnemyPlayerShooter
        EnemyPlayerShooter pShooter = enemy.GetComponent<EnemyPlayerShooter>();
        if (pShooter != null)
        {
            pShooter.currentPhase = currentPhase;
            pShooter.arenaMinX = arenaMinX;
            pShooter.arenaMaxX = arenaMaxX;
        }
    }

    Vector3 GetRandomSpawnPos()
    {
        float x = Random.Range(arenaMinX + 2f, arenaMaxX - 2f);
        float y = Random.Range(arenaMinY, arenaMaxY);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Chamado quando o cenário recebe dano (projéteis AntiCorpo).
    /// </summary>
    public void ApplyOrganDamage(float rawDamage)
    {
        if (isOrganDead) return;

        organCurrentHP -= rawDamage;
        float bodyDamage = rawDamage * damageMultiplier;
        bodyCurrentHP -= bodyDamage;

        if (organCurrentHP <= 0f)
        {
            organCurrentHP = 0f;
            isOrganDead = true;
        }

        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
        }
    }

    void OnPlayerDied() => GameOver("GLÓBULO BRANCO DESTRUÍDO");

    void GameOver(string reason)
    {
        isGameOver = true;
        Debug.Log($"GAME OVER: {reason}");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Exibir motivo se o texto estiver configurado
            if (gameOverReasonText != null)
                gameOverReasonText.text = reason;
            else
            {
                // Tentar encontrar texto de razão pelo nome
                var goSub = gameOverPanel.transform.Find("GOSub");
                if (goSub != null)
                {
                    var txt = goSub.GetComponent<Text>();
                    if (txt != null) txt.text = reason;
                }
            }
        }

        // Parar time
        StartCoroutine(SlowTimeAndStop());
    }

    IEnumerator SlowTimeAndStop()
    {
        float duration = 1f, elapsed = 0f;
        while (elapsed < duration)
        {
            Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 0f;
    }

    void OnPhaseCompleted()
    {
        Debug.Log($"Fase {currentPhase} completada! Órgão morto: {isOrganDead}");
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartPhase()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Utilitário: busca botão pelo nome dentro de um Transform e conecta a ação.
    /// Funciona mesmo em objetos inativos (Transform.Find não depende de SetActive).
    /// </summary>
    void WireButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        Button btn = child.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }
}
