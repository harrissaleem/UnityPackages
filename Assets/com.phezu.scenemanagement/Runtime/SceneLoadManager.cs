using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Phezu.Util;

namespace Phezu.SceneManagingSystem
{
    [AddComponentMenu("Phezu/Scene Managing System/Scene Load Manager")]
    public class SceneLoadManager : Singleton<SceneLoadManager>
    {
        public delegate void SceneLoadBeginAction(string sceneName);
        public event SceneLoadBeginAction OnSceneLoadBegin;

        public delegate void SceneLoadEndAction(string sceneName);
        public event SceneLoadEndAction OnSceneLoadEnd;

        public GameObject logo;
        private bool isloading;

        public string currentLoadedScene;

        private void Awake()
        {
            currentLoadedScene = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += SetActiveScene;
        }

        public void LoadNewScene(string sceneName, bool unloadCurrent)
        {
            if (!isloading)
                StartCoroutine(LoadScene(sceneName, unloadCurrent));
            else
                Debug.Log("Scene is already loading");
        }

        private IEnumerator LoadScene(string sceneName, bool unloadCurrent)
        {
            isloading = true;
            OnSceneLoadBegin?.Invoke(sceneName);
            // fade out screen here
            if (unloadCurrent)
            {
                // Fade out here;
                // show logo if needed;
                // yield return new WaitForSeconds(fadeTime);
                yield return StartCoroutine(UnloadCurrent());
            }

            yield return StartCoroutine(LoadNewAsync(sceneName));
            //fade in screen here
            if (unloadCurrent)
            {
                // Fade in here
                // yield return new WaitForSeconds(fadeTime);
                // disable logo here
            }
            currentLoadedScene = sceneName;
            OnSceneLoadEnd?.Invoke(sceneName);

            isloading = false;
            yield return null;
        }

        private IEnumerator UnloadCurrent()
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            while (!unloadOperation.isDone)
                yield return null;
        }

        private IEnumerator LoadNewAsync(string sceneName)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadOperation.isDone)
                yield return null;
        }

        private void SetActiveScene(Scene scene, LoadSceneMode mode)
        {
            SceneManager.SetActiveScene(scene);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SetActiveScene;
        }
    }
}