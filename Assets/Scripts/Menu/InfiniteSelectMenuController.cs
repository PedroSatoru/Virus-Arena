using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador da tela de seleção de fase do Modo Infinito.
/// Exibe 3 caixas grandes (Fase 1 / 2 / 3) com o recorde salvo de cada uma.
/// </summary>
public class InfiniteSelectMenuController : MonoBehaviour
{
    [Header("Botões de seleção de fase")]
    public Button phase1Button;
    public Button phase2Button;
    public Button phase3Button;

    [Header("Textos de recorde")]
    public Text phase1RecordText;
    public Text phase2RecordText;
    public Text phase3RecordText;

    [Header("Botão voltar")]
    public Button backButton;

    void Start()
    {
        // Conectar botões
        if (phase1Button != null) phase1Button.onClick.AddListener(() => StartInfinitePhase(1));
        if (phase2Button != null) phase2Button.onClick.AddListener(() => StartInfinitePhase(2));
        if (phase3Button != null) phase3Button.onClick.AddListener(() => StartInfinitePhase(3));
        if (backButton   != null) backButton.onClick.AddListener(GoBack);

        // Exibir records
        UpdateRecordTexts();
    }

    void UpdateRecordTexts()
    {
        UpdateRecord(phase1RecordText, InfiniteGameManager.PREF_KEY_PH1);
        UpdateRecord(phase2RecordText, InfiniteGameManager.PREF_KEY_PH2);
        UpdateRecord(phase3RecordText, InfiniteGameManager.PREF_KEY_PH3);
    }

    void UpdateRecord(Text target, string prefKey)
    {
        if (target == null) return;
        float record = PlayerPrefs.GetFloat(prefKey, 0f);
        if (record <= 0f)
            target.text = "RECORDE: --:--";
        else
            target.text = $"RECORDE: {FormatTime(record)}";
    }

    void StartInfinitePhase(int phase)
    {
        GlobalState.ResetInfiniteRun();
        GlobalState.isInfiniteMode = true;
        GlobalState.infinitePhase  = phase;

        string sceneName = phase switch
        {
            2 => "GameScene_Inf2",
            3 => "GameScene_Inf3",
            _ => "GameScene_Inf1",
        };

        SceneManager.LoadScene(sceneName);
    }

    void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:D2}", m, s);
    }
}
