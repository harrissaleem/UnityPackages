using UnityEngine;
using ZoiStudio.InputManager;

public class UIInputHandler : MonoBehaviour, IInputListener
{
    public void OnInput(InputActionArgs input)
    {
        switch (input.Action)
        {
            case GameAction.Tap:
                break;
            case GameAction.TapReleased:
                break;
            case GameAction.Hold:
                break;
            case GameAction.SwipeLeft:
                break;
            case GameAction.SwipeRight:
                break;
            case GameAction.SwipeUp:
                break;
            case GameAction.SwipeDown:
                break;
            default:
                break;
        }
    }
}
