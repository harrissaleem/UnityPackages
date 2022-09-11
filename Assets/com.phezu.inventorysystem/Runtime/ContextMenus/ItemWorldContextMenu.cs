using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Phezu.Util;

namespace Phezu.InventorySystem.ContextMenus
{
    public class ItemWorldContextMenu : Singleton<ItemWorldContextMenu>
    {
        [SerializeField] private Button mTopButton;
        [SerializeField] private Button mRightButton;
        [SerializeField] private Button mBottomButton;
        [SerializeField] private Button mLeftButton;
        [SerializeField] private TextMeshProUGUI mTopText;
        [SerializeField] private TextMeshProUGUI mRightText;
        [SerializeField] private TextMeshProUGUI mBottomText;
        [SerializeField] private TextMeshProUGUI mLeftText;

        private Canvas mCanvas;

        private void Start()
        {
            ((RectTransform)transform).ForceUpdateRectTransforms();
            mCanvas = GetComponent<Canvas>();
            Disable();
        }

        public void SetTopButton(string buttonText, UnityAction onClick)
        {
            SetButton(mTopButton, mTopText, buttonText, onClick);
        }
        public void SetBottomButton(string buttonText, UnityAction onClick)
        {
            SetButton(mBottomButton, mBottomText, buttonText, onClick);
        }
        public void SetLeftButton(string buttonText, UnityAction onClick)
        {
            SetButton(mLeftButton, mLeftText, buttonText, onClick);
        }
        public void SetRightButton(string buttonText, UnityAction onClick)
        {
            SetButton(mRightButton, mRightText, buttonText, onClick);
        }
        private void SetButton(Button buttonToSet, TextMeshProUGUI textToSet, string buttonText, UnityAction onClick)
        {
            textToSet.text = buttonText;
            buttonToSet.onClick.RemoveAllListeners();
            buttonToSet.onClick.AddListener(onClick);
        }

        public void Enable()
        {
            mCanvas.enabled = true;
        }
        public void Disable()
        {
            mCanvas.enabled = false;
        }
    }
}