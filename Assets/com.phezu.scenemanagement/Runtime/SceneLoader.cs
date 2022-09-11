using UnityEngine;

namespace Phezu.SceneManagingSystem
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad;

        private void Start()
        {
            SceneLoadManager.Instance.LoadNewScene(sceneToLoad, false);
        }
    }
}