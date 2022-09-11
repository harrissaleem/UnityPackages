using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZoiStudio.InputManager
{
    public class InputControlHandler<T> where T : struct
    {
        private static Hashtable mSortedListeners = new Hashtable();
        private static string mCurrControllingGroup = null;
        private static List<string> mActiveGroups = new List<string>();

        public static void RegisterAsInputListener(IInputListener<T> listener)
        {
            var array = new List<IInputListener<T>>();
            bool isInActivatedGroup = listener.ListenerGroup.CompareTo(mCurrControllingGroup) == 0;
            foreach (string group in mActiveGroups)
                isInActivatedGroup |= listener.ListenerGroup.CompareTo(group) == 0;

            if (mSortedListeners.ContainsKey(listener.ListenerGroup))
            {
                array = (List<IInputListener<T>>)mSortedListeners[listener.ListenerGroup];
                if (!array.Contains(listener))
                {
                    array.Add(listener);
                    if (isInActivatedGroup)
                        listener.Activate();
                }
                return;
            }
            array.Add(listener);
            mSortedListeners.Add(listener.ListenerGroup, array);
            if (isInActivatedGroup)
                listener.Activate();
        }

        public static void TransferControl(string listenerGroup)
        {
            if (!mSortedListeners.ContainsKey(listenerGroup))
            {
                Debug.Log("ListenerGroup = " + listenerGroup + " for T = " + typeof(T).ToString() + " does not exist");
		mCurrControllingGroup = listenerGroup;
                return;
            }
            if (mCurrControllingGroup != null)
            {
                var prevGroup = (List<IInputListener<T>>)mSortedListeners[mCurrControllingGroup];
                foreach (var listener in prevGroup)
                    listener.Deactivate();
            }
            var newGroup = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
            foreach (var listener in newGroup)
                listener.Activate();
            mCurrControllingGroup = listenerGroup;
        }

        public static void ActivateGroup(string listenerGroup)
        {
            if (!mSortedListeners.ContainsKey(listenerGroup))
            {
                mSortedListeners.Add(listenerGroup, new List<IInputListener<T>>());
                mActiveGroups.Add(listenerGroup);
                return;
            }
            if (mActiveGroups.Contains(listenerGroup))
                return;
            mActiveGroups.Add(listenerGroup);
            var newGroup = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
            foreach (var listener in newGroup)
                listener.Activate();
        }
    }
}