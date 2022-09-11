using UnityEngine;

namespace ZoiStudio.SceneManagingSystem
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