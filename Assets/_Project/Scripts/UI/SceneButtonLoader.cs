using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SceneButtonLoader : MonoBehaviour
    {
        [SerializeField] private string sceneName = "MainMenu";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.RemoveListener(LoadScene);
            _button.onClick.AddListener(LoadScene);
        }

        public void LoadScene()
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
