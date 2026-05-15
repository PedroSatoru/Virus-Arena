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
    public GameObject kamikazePrefab;        // Kamikaze (Laranja/Vermelho escuro)

    [Header("Boss (Fase 3)")]
    public GameObject bossPrefab;
    public GameObject bossHealthPanel;
    private BossController activeBoss;
    private bool bossSpawned = false;

    [Header("Escalonamento por Tempo")]
    public float speedMultiplier = 1f;

    [Header("Spawn de Inimigos")]
    public float baseSpawnInterval = 2.5f;
    public float minSpawnInterval = 1f;
    public int maxTotalEnemies = 8;          // Máximo total em tela
    public int maxPerType = 3;               // Máximo de cada tipo

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
    public Text gameOverReasonText;

    [Header("UI Vitória")]
    public GameObject victoryPanel;

    [Header("UI Power Up (Apenas fases 1 e 2)")]
    public GameObject powerUpPanel;

    // Spawn state
    private float spawnTimer;
    private int nextEnemyTypeIndex = 0; // Alterna entre tipos de inimigo

    // --- Audio ---
    private AudioSource bgmSource;
    private AudioClip bgmClip;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (GlobalState.hasHeartAndBodyHP)
            bodyMaxHP *= 1.3f;

        if (GlobalState.savedBodyHP >= 0f)
            bodyCurrentHP = GlobalState.savedBodyHP;
        else
            bodyCurrentHP = bodyMaxHP;

        timeRemaining = totalTime;
        organCurrentHP = organMaxHP;
        damageMultiplier = currentPhase;
        spawnTimer = 2f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (powerUpPanel != null) powerUpPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);

        // Auto-fiação dos botões da Vitória
        if (victoryPanel != null)
        {
            WireButton(victoryPanel.transform, "VictoryMenuBtn", ReturnToMenu);
        }

        // Auto-fiação dos botões do Game Over (via Transform.Find - funciona em objetos inativos)
        if (gameOverPanel != null)
        {
            WireButton(gameOverPanel.transform, "RestartBtn", RestartPhase);
            WireButton(gameOverPanel.transform, "GoMenuBtn", ReturnToMenu);
        }

        if (powerUpPanel != null)
        {
            WireButton(powerUpPanel.transform, "PowerUpBox/TripleBtn", ChoosePowerUpTripleShot);
            WireButton(powerUpPanel.transform, "PowerUpBox/SpeedBtn", ChoosePowerUpSpeedBoost);
            WireButton(powerUpPanel.transform, "PowerUpBox/LifeBtn", ChoosePowerUpHeartAndBody);
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

        // Intro cutscene (apenas Fase 1)
        if (currentPhase == 1)
        {
            CutsceneManager cs = FindFirstObjectByType<CutsceneManager>();
            if (cs != null)
            {
                Time.timeScale = 0f; // Pausa enquanto mostra a intro
                cs.Show("inicioJogo", "COMEÇAR ▶", () => { Time.timeScale = 1f; });
            }
        }

        // --- Configurar Música de Fundo ---
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmClip = Resources.Load<AudioClip>("Audio/musica_fases");
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0.4f;
            bgmSource.pitch = 0.9f;
            bgmSource.Play();
        }
    }

    void Update()
    {
        if (isGameOver || isPaused) return;

        // Tempo e Speed Scaling
        timeRemaining -= Time.deltaTime;
        float minutesPassed = (totalTime - timeRemaining) / 60f;
        speedMultiplier = 1f + (minutesPassed * 0.1f); // 10% de aumento por minuto

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            OnPhaseCompleted();
            return;
        }

        // Não spawnar inimigos normais se o boss já apareceu
        if (bossSpawned) return;

        // Spawn (agora escala com velocidade, ou seja, nasce mais rápido)
        spawnTimer -= Time.deltaTime * speedMultiplier;
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
        int kamikazeCount = FindObjectsByType<EnemyKamikaze>(FindObjectsSortMode.None).Length;
        int totalCount = antiCorpoCount + playerShooterCount + kamikazeCount;

        if (totalCount >= maxTotalEnemies) return;

        // Amarelos (AntiCorpo) devem ter preferência, então não ficam restritos ao maxPerType
        bool canSpawnAntiCorpo = antiCorpoPrefab != null && antiCorpoCount < maxTotalEnemies;
        bool canSpawnPlayerShooter = playerShooterPrefab != null && playerShooterCount < maxPerType;
        bool canSpawnKamikaze = kamikazePrefab != null && kamikazeCount < maxPerType;

        if (!canSpawnAntiCorpo && !canSpawnPlayerShooter && !canSpawnKamikaze) return;

        GameObject chosenPrefab = null;
        
        // Probabilidade ponderada: 60% Amarelo, 20% Roxo, 20% Kamikaze
        float roll = Random.value;
        if (roll < 0.6f && canSpawnAntiCorpo) chosenPrefab = antiCorpoPrefab;
        else if (roll < 0.8f && canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
        else if (canSpawnKamikaze) chosenPrefab = kamikazePrefab;
        
        // Fallback
        if (chosenPrefab == null)
        {
            if (canSpawnAntiCorpo) chosenPrefab = antiCorpoPrefab;
            else if (canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
            else if (canSpawnKamikaze) chosenPrefab = kamikazePrefab;
        }

        nextEnemyTypeIndex++;

        // Escolher posição baseada no inimigo
        Vector3 spawnPos;
        if (chosenPrefab == kamikazePrefab)
        {
            Vector3 playerPos = Vector3.zero;
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerPos = pObj.transform.position;
            spawnPos = GetSafeKamikazeSpawnPos(playerPos);
        }
        else
        {
            spawnPos = GetRandomSpawnPos();
        }

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

    Vector3 GetSafeKamikazeSpawnPos(Vector3 playerPos)
    {
        // Spawnar no quadrante oposto ao player
        float spawnX, spawnY;

        if (playerPos.x < 0)
            spawnX = Random.Range(2f, arenaMaxX - 1f); // Player esquerda -> Spawn direita
        else
            spawnX = Random.Range(arenaMinX + 1f, -2f); // Player direita -> Spawn esquerda

        // O Y do player geralmente cai entre -4.5 e 4.5. Vamos assumir 0 como meio
        if (playerPos.y < 0)
            spawnY = Random.Range(1f, arenaMaxY - 1f); // Player embaixo -> Spawn em cima
        else
            spawnY = Random.Range(-3f, -1f); // Player em cima -> Spawn embaixo
            
        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// Chamado quando o cenário recebe dano (projéteis AntiCorpo).
    /// </summary>
    public void ApplyOrganDamage(float rawDamage)
    {
        if (isOrganDead || isGameOver) return;

        organCurrentHP -= rawDamage;
        float bodyDamage = rawDamage * damageMultiplier;
        bodyCurrentHP -= bodyDamage;

        // PRIORIDADE: Se o corpo morreu, é Game Over imediato, ignorando se o órgão também morreu
        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
            return;
        }

        if (organCurrentHP <= 0f)
        {
            organCurrentHP = 0f;
            if (!isOrganDead)
            {
                isOrganDead = true;
                GlobalState.organLostPhase = currentPhase;

                // Fases 1 e 2: se o órgão morre, avança automaticamente
                if (currentPhase <= 2)
                {
                    OnPhaseCompleted();
                    return;
                }
                else if (currentPhase == 3)
                {
                    // Fase 3: Se o cérebro morre, o jogo acaba imediatamente com o final ruim
                    OnBossDefeated(); 
                    return;
                }
            }
        }
    }

    void OnPlayerDied() => GameOver("GLÓBULO BRANCO DESTRUÍDO");

    void GameOver(string reason)
    {
        isGameOver = true;
        Debug.Log($"GAME OVER: {reason}");

        // Mostrar cutscene de morte antes do painel de Game Over
        CutsceneManager cs = FindFirstObjectByType<CutsceneManager>();
        if (cs != null)
        {
            cs.Show("FimMorte", "VOLTAR AO MENU", () =>
            {
                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(true);
                    var goSub = gameOverPanel.transform.Find("GOSub");
                    if (goSub != null)
                    {
                        var txt = goSub.GetComponent<Text>();
                        if (txt != null) txt.text = reason;
                    }
                }
                ReturnToMenu();
            });
        }
        else
        {
            // Fallback sem cutscene
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (gameOverReasonText != null)
                    gameOverReasonText.text = reason;
                else
                {
                    var goSub = gameOverPanel.transform.Find("GOSub");
                    if (goSub != null)
                    {
                        var txt = goSub.GetComponent<Text>();
                        if (txt != null) txt.text = reason;
                    }
                }
            }
            StartCoroutine(SlowTimeAndStop());
        }
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

        // Fases 1 e 2: mostrar painel de PowerUp antes de avançar
        if (currentPhase <= 2)
        {
            isGameOver = true;
            if (powerUpPanel != null)
            {
                // Desabilitar botões de powerups já escolhidos (exceto vida extra)
                UpdatePowerUpButtons();
                
                powerUpPanel.SetActive(true);
                StartCoroutine(SlowTimeAndStop());
            }
        }
        else if (currentPhase == 3 && !bossSpawned)
        {
            // Fase 3: spawnar o Boss ao invés de terminar
            SpawnBoss();
        }
    }

    /// <summary>
    /// Desabilita visualmente e funcionalmente botões de PowerUp já adquiridos.
    /// Triple Shot e Speed Boost só podem ser pegos 1x.
    /// </summary>
    void UpdatePowerUpButtons()
    {
        if (powerUpPanel == null) return;

        Transform box = powerUpPanel.transform.Find("PowerUpBox");
        if (box == null) return;

        // Triple Shot
        if (GlobalState.hasTripleShot)
        {
            Transform btn = box.Find("TripleBtn");
            if (btn != null)
            {
                Button b = btn.GetComponent<Button>();
                b.interactable = false;
                btn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                var txt = btn.Find("TripleBtn_Txt")?.GetComponent<Text>();
                if (txt != null) txt.text = "[JÁ ADQUIRIDO]";
            }
        }

        // Speed Boost
        if (GlobalState.hasSpeedBoost)
        {
            Transform btn = box.Find("SpeedBtn");
            if (btn != null)
            {
                Button b = btn.GetComponent<Button>();
                b.interactable = false;
                btn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                var txt = btn.Find("SpeedBtn_Txt")?.GetComponent<Text>();
                if (txt != null) txt.text = "[JÁ ADQUIRIDO]";
            }
        }
    }

    void SpawnBoss()
    {
        if (bossPrefab == null) return;

        bossSpawned = true;
        Debug.Log("💀 BOSS APARECEU!");

        // Destruir todos os inimigos normais
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e.GetComponent<BossController>() == null)
                Destroy(e.gameObject);
        }

        // Instanciar boss no centro da tela
        GameObject bossObj = Instantiate(bossPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
        activeBoss = bossObj.GetComponent<BossController>();

        if (activeBoss != null)
        {
            activeBoss.OnBossDefeated += OnBossDefeated;

            // Conectar UI do boss
            if (bossHealthPanel != null)
            {
                bossHealthPanel.SetActive(true);
                activeBoss.bossHealthSlider = bossHealthPanel.GetComponentInChildren<Slider>();
                activeBoss.bossHealthFill = bossHealthPanel.transform.Find("BossHealthSlider/Fill Area/Fill")?.GetComponent<UnityEngine.UI.Image>();
                activeBoss.bossHealthLabel = bossHealthPanel.transform.Find("BossLabel")?.GetComponent<Text>();
            }

            activeBoss.ActivateBoss();
        }
    }

    void OnBossDefeated()
    {
        isGameOver = true;
        Debug.Log("🎉 BOSS DERROTADO! VITÓRIA!");

        // Escolher cutscene de fim baseada nos órgãos perdidos
        // Prioridade: Cérebro (Ph3) > Coração (Ph2) > Pulmão (Ph1)
        string cutsceneName;
        if (isOrganDead || GlobalState.organLostPhase == 3) 
            cutsceneName = "FimCerebro";
        else if (GlobalState.organLostPhase == 2)
            cutsceneName = "FimCoração";
        else if (GlobalState.organLostPhase == 1)
            cutsceneName = "FimPulmao";
        else
            cutsceneName = "FinalFeliz"; // vitória perfeita

        CutsceneManager cs = FindFirstObjectByType<CutsceneManager>();
        if (cs != null)
        {
            cs.Show(cutsceneName, "MENU PRINCIPAL", () =>
            {
                if (victoryPanel != null) victoryPanel.SetActive(true);
                StartCoroutine(SlowTimeAndStop());
            });
        }
        else
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            StartCoroutine(SlowTimeAndStop());
        }
    }

    /// <summary>
    /// Dano direto do Boss ao corpo e órgão (bypassa o sistema normal de arena).
    /// </summary>
    public void ApplyBossDamage(float damage)
    {
        bodyCurrentHP -= damage;
        organCurrentHP -= damage * 0.5f; // 50% do dano também vai pro órgão (25 de dano)

        if (organCurrentHP <= 0f) 
        {
            organCurrentHP = 0f;
            if (!isOrganDead)
            {
                isOrganDead = true;
                GlobalState.organLostPhase = 3;
                
                // Se o cérebro morre durante o boss, acaba imediatamente
                OnBossDefeated();
                return;
            }
        }

        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
        }
    }

    // ==== Escolhas de PowerUP ====
    public void ChoosePowerUpTripleShot()
    {
        GlobalState.hasTripleShot = true;
        AdvanceToNextPhase();
    }

    public void ChoosePowerUpSpeedBoost()
    {
        GlobalState.hasSpeedBoost = true;
        AdvanceToNextPhase();
    }

    public void ChoosePowerUpHeartAndBody()
    {
        GlobalState.hasHeartAndBodyHP = true;
        
        // Aumenta vida limitando estrito e recupera exatamente o tanto que aumentou
        float gain = bodyMaxHP * 0.3f;
        GlobalState.savedBodyHP = bodyCurrentHP + gain;

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            GlobalState.savedPlayerHearts = ph.currentHearts + 1;
        else
            GlobalState.savedPlayerHearts = 6;
            
        AdvanceToNextPhase();
    }

    void AdvanceToNextPhase()
    {
        Time.timeScale = 1f;
        if (!GlobalState.hasHeartAndBodyHP)
        {
            GlobalState.savedBodyHP = bodyCurrentHP;
            PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null) GlobalState.savedPlayerHearts = ph.currentHearts;
        }

        // Avança de cena. Futuramente Fase 3 será tratada aqui (se currentPhase == 2, load Ph3)
        if (currentPhase == 1)
        {
            GlobalState.currentPhase = 2; 
            SceneManager.LoadScene("GameScene_Ph2");
        }
        else if (currentPhase == 2)
        {
            GlobalState.currentPhase = 3;
            SceneManager.LoadScene("GameScene_Ph3");
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        GlobalState.ResetState();
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartPhase()
    {
        Time.timeScale = 1f;
        // NÃO chamamos GlobalState.ResetState() aqui para não perder 
        // os PowerUps e o histórico de órgãos perdidos da run atual.
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
