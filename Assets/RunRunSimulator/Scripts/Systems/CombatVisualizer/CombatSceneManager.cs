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

    private Button homeButton;

    private void Start()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (document == null) { Debug.LogWarning("[CombatSceneManager] No UIDocument."); return; }
        var root = document.rootVisualElement;
        if (root == null) return;
        homeButton = root.Q<Button>("btn-home");
        if (homeButton != null) homeButton.clicked += ReturnToGameScene;
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
