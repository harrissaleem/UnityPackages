using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This is an alternate approach of Action Invoke(). As Invoke would use reflection which is not good for performance
/// </summary>
public class InputEventManager
{
    private static Hashtable _listenerTable = new Hashtable();
    public static bool Subscribe(IInputListener listener, params GameAction[] actions)
    {
        if (listener == null)
        {
            Debug.LogError("Specified listener is null");
            return false;
        }

		foreach (GameAction action in actions)
		{
            if (!_listenerTable.ContainsKey(action))
            {
                _listenerTable.Add(action, new ArrayList());
            }

            ArrayList listenerList = _listenerTable[action] as ArrayList;
            if (listenerList == null)
            {
                Debug.LogError("listerner list is null, this is technically impossible");
                return false;
            }

            if (listenerList.Contains(listener))
			{
                Debug.LogError("listener is already present in the list. You shouldn't try to subscribe again");
                return false;
            }
            listenerList.Add(listener);
        }
        return true;
    }

    public static bool UnSubscribe(IInputListener listener, params GameAction[] actions)
    {
        if (listener == null)
        {
            Debug.LogError("Specified listener is null");
            return false;
        }

		foreach (GameAction action in actions)
		{
			if (!_listenerTable.ContainsKey(action))
			{
				Debug.LogError("No listeners attached to this event name " + action.ToString());
				return false;
			}

			ArrayList listenerList = _listenerTable[action] as ArrayList;
			if (listenerList == null)
			{
				Debug.LogError("Listener list is null this is impossible at this stage");
				return false;
			}

			if (!listenerList.Contains(listener))
			{
				Debug.LogError("this listener is not part of this event's listener list");
				return false;
			}

			listenerList.Remove(listener); 
		}
        return true;
    }

    public static bool Invoke(InputActionArgs actionArgs)
    {        
        if (!_listenerTable.ContainsKey(actionArgs.Action))
        {
            //no listeners for this event so ignore it
            Debug.LogWarning("no listeners for event: " + actionArgs.Action);
            return false;
        }

        ArrayList listenerList = _listenerTable[actionArgs.Action] as ArrayList;
        if (listenerList == null)
        {
            Debug.LogError("listener list can never be null: " + actionArgs.Action);
            return false;
        }

        // Not using foreach because listenerList count can change during the execution
        for (int i = 0; i <= listenerList.Count - 1; i ++)
        {           
            IInputListener listener = listenerList[i] as IInputListener;
            if (listener == null)
            {
                //remove null listener and continue to next one
                listenerList.RemoveAt(i);
                continue;
            }
            listener.OnInput(actionArgs);
        }
        return true;
    }
}
