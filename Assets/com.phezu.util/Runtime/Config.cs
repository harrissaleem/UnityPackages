using UnityEngine;

namespace Phezu.Util
{
    public class Config : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 200;
            QualitySettings.vSyncCount = 0;
        }
    }
}