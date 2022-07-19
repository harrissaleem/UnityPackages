using UnityEngine;
using ZoiStudio.InputManager;

public class UIInputHandler : MonoBehaviour, IInputListener<TouchData>
{
    public void OnInput(InputActionArgs<TouchData> input)
    {
        switch (input.Action)
        {
            case TouchGameAction.Tap:
                break;
            case TouchGameAction.TapReleased:
                break;
            case TouchGameAction.Hold:
                break;
            case TouchGameAction.SwipeLeft:
                break;
            case TouchGameAction.SwipeRight:
                break;
            case TouchGameAction.SwipeUp:
                break;
            case TouchGameAction.SwipeDown:
                break;
            default:
                break;
        }
    }
}
