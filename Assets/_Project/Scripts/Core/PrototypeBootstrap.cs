using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLetThemIn.Core
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameManagerExists()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "GameScene")
            {
                return;
            }

            if (Object.FindObjectOfType<GameManager>() != null)
            {
                return;
            }

            GameObject gameManagerObject = new("GameManager");
            gameManagerObject.AddComponent<GameManager>();
        }
    }
}
