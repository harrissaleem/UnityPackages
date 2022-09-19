using System;
using System.Collections;
using UnityEngine;

namespace ZoiStudio.InputManager
{
    /// <summary>
    /// This is an alternate approach of Action Invoke(). As Invoke would use reflection which is not good for performance
    /// </summary>
    public class InputEventManager<T> where T : struct
    {
        private static Hashtable _listenerTable = new Hashtable();

        // Table specific for elements that want to know if they have been tapped
        private static Hashtable _uiListenerTable = new Hashtable();

        public static bool Register(IInputListener<T> listener, params Enum[] actions)
        {
            return AddListener(_uiListenerTable, listener, actions);
        }

        public static bool Subscribe(IInputListener<T> listener, params Enum[] actions)
        {
            return AddListener(_listenerTable, listener, actions);
        }

        private static bool AddListener(Hashtable hashtable, IInputListener<T> listener, params Enum[] actions)
        {
            if (listener == null)
            {
                Debug.Log("Specified listener is null");
                return false;
            }

            foreach (Enum action in actions)
            {
                if (!hashtable.ContainsKey(action))
                {
                    hashtable.Add(action, new ArrayList());
                }

                ArrayList listenerList = hashtable[action] as ArrayList;
                if (listenerList == null)
                {
                    Debug.Log("listerner list is null, this is technically impossible");
                    return false;
                }

                if (listenerList.Contains(listener))
                {
                    Debug.Log("listener is already present in the list. You shouldn't try to subscribe again");
                    return false;
                }
                listenerList.Add(listener);
            }
            return true;
        }

        private static bool RemoveListener(Hashtable hashtable, IInputListener<T> listener, params Enum[] actions)
        {
            if (listener == null)
            {
                Debug.Log("Specified listener is null");
                return false;
            }

            foreach (Enum action in actions)
            {
                if (!hashtable.ContainsKey(action))
                {
                    Debug.Log("No listeners attached to this event name " + action.ToString());
                    return false;
                }

                ArrayList listenerList = hashtable[action] as ArrayList;
                if (listenerList == null)
                {
                    Debug.Log("Listener list is null this is impossible at this stage");
                    return false;
                }

                if (!listenerList.Contains(listener))
                {
                    Debug.Log("this listener is not part of this event's listener list");
                    return false;
                }

                listenerList.Remove(listener);
            }
            return true;
        }

        public static bool UnRegister(IInputListener<T> listener, params Enum[] actions)
        {
            return RemoveListener(_uiListenerTable, listener, actions);
        }

        public static bool UnSubscribe(IInputListener<T> listener, params Enum[] actions)
        {
            return RemoveListener(_listenerTable, listener, actions);
        }

        public static bool Invoke(InputActionArgs<T> actionArgs)
        {
            InvokeValidListeners(_listenerTable, actionArgs);

            return true;
        }

        private static ArrayList InvokeValidListeners(Hashtable table, InputActionArgs<T> actionArgs)
        {
            if (!table.ContainsKey(actionArgs.Action))
            {
                //no listeners for this event so ignore it
                Debug.LogWarning("no listeners for event: " + actionArgs.Action);
                return null;
            }

            ArrayList listenerList = table[actionArgs.Action] as ArrayList;
            if (listenerList == null)
            {
                Debug.Log("listener list can never be null: " + actionArgs.Action);
                return null;
            }

            // Not using foreach because listenerList count can change during the execution
            for (int i = 0; i <= listenerList.Count - 1; i++)
            {
                IInputListener<T> listener = listenerList[i] as IInputListener<T>;
                if (listener == null)
                {
                    //remove null listener and continue to next one
                    listenerList.RemoveAt(i);
                    continue;
                }
                listener.OnInput(actionArgs);
            }

            return listenerList;
        }
    }
}