using UnityEngine;

namespace ZoiStudio.Util
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