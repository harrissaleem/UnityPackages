using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZoiStudio.InputManager;

// This is just a sample. You should implement your own input handler class
public class InputHandler : MonoBehaviour, IInputListener<TouchData>
{
    public string ListenerGroup { get; private set; }
    public List<InputAction> gameActions;

    public void Activate()
    {
        InputEventManager<TouchData>.Subscribe(this, gameActions.Select(x => x.Action).ToArray());
    }

    public void Deactivate()
    {
        InputEventManager<TouchData>.UnSubscribe(this, gameActions.Select(x => x.Action).ToArray());
    }

    public void OnInput(InputActionArgs<TouchData> action)
    {
        var inputAction = gameActions.Where(x => x.Action == action.Action).First();
        if (inputAction.OnAction != null)
        {
            inputAction.OnAction.Invoke();
        }
    }
}
