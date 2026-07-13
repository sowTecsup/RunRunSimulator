using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class CombatSceneManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField, Tooltip("Nombre de la escena a la que vuelve el botón (debe estar en Build Settings).")]
    private string gameSceneName = "GameScene";

    private const float ReplayResolveTimeout = 3f;

    private Button homeButton;

    private void Start()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (document == null) { Debug.LogWarning("[CombatSceneManager] No UIDocument."); return; }
        var root = document.rootVisualElement;
        if (root == null) return;
        homeButton = root.Q<Button>("btn-home");
        if (homeButton != null) homeButton.clicked += ReturnToGameScene;

        if (CombatReplayRequest.Pending)
            StartCoroutine(ConsumeReplayRequest());
    }

    private IEnumerator ConsumeReplayRequest()
    {
        float elapsed = 0f;
        while ((GameManager.Instance == null || GameManager.Instance.Registry == null) && elapsed < ReplayResolveTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (GameManager.Instance == null || GameManager.Instance.Registry == null)
        {
            Debug.LogWarning("[CombatSceneManager] Timeout esperando GameManager/Registry para el replay.");
            CombatReplayRequest.Clear();
            yield break;
        }

        CreatureDNA self = null;
        elapsed = 0f;
        while (!GameManager.Instance.Registry.TryGet(CombatReplayRequest.SelfId, out self) && elapsed < ReplayResolveTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (self == null)
        {
            Debug.LogWarning($"[CombatSceneManager] No se encontró la criatura '{CombatReplayRequest.SelfId}' en el registro para el replay.");
            CombatReplayRequest.Clear();
            yield break;
        }

        int fightIndex = CombatReplayRequest.FightIndex;
        if (self.CombatHistory == null || fightIndex < 0 || fightIndex >= self.CombatHistory.Count)
        {
            Debug.LogWarning("[CombatSceneManager] Índice de pelea inválido para el replay.");
            CombatReplayRequest.Clear();
            yield break;
        }

        var record = self.CombatHistory[fightIndex];

        CombatReplayRequest.Clear();
        CombatVisualizerService.Instance?.Play(self, record);
    }

    private void OnDisable()
    {
        if (homeButton != null) homeButton.clicked -= ReturnToGameScene;
    }

    [Button("Volver a GameScene"), DisableInEditorMode]
    public void ReturnToGameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[CombatSceneManager] gameSceneName vacío.");
            return;
        }
        CombatVisualizerService.Instance?.Stop();
        SceneManager.LoadScene(gameSceneName);
    }
}
}
