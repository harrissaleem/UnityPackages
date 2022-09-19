using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZoiStudio.InputManager {
    public class InputControlHandler<T> where T : struct {
        /// <summary>
        /// Contains all listeners by their listener group.
        /// </summary>
        private static Hashtable mSortedListeners = new Hashtable();
        private static string mCurrControllingGroup = null;
        private static List<string> mActiveGroups = new List<string>();

        public static void RegisterAsInputListener(IInputListener<T> listener) {
            var array = new List<IInputListener<T>>();
            bool isInActivatedGroup = listener.ListenerGroup.CompareTo(mCurrControllingGroup) == 0;
            foreach (string group in mActiveGroups)
                isInActivatedGroup |= listener.ListenerGroup.CompareTo(group) == 0;

            if (mSortedListeners.ContainsKey(listener.ListenerGroup)) {
                array = (List<IInputListener<T>>)mSortedListeners[listener.ListenerGroup];
                if (!array.Contains(listener)) {
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

        public static void UnRegisterAsInputListener(IInputListener<T> listener) {
            if (IsListenerActive(listener))
                listener.Deactivate();

            string listenerGroup = listener.ListenerGroup;

            if (mSortedListeners.ContainsKey(listenerGroup)) {
                var array = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
                if (array.Contains(listener))
                    array.Remove(listener);
            }
        }

        public static void TransferControl(string listenerGroup) {
            if (!mSortedListeners.ContainsKey(listenerGroup)) {
                Debug.Log("ListenerGroup = " + listenerGroup + " for T = " + typeof(T).ToString() + " does not exist");
                mCurrControllingGroup = listenerGroup;
                return;
            }
            if (mCurrControllingGroup != null) {
                var prevGroup = (List<IInputListener<T>>)mSortedListeners[mCurrControllingGroup];
                DeactivateGroup(prevGroup);
            }
            var groupToActivate = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
            ActivateGroup(groupToActivate);
            mCurrControllingGroup = listenerGroup;
        }

        public static void ActivateGroup(string listenerGroup) {
            if (!mSortedListeners.ContainsKey(listenerGroup)) {
                mSortedListeners.Add(listenerGroup, new List<IInputListener<T>>());
                mActiveGroups.Add(listenerGroup);
                return;
            }
            if (mActiveGroups.Contains(listenerGroup))
                return;
            mActiveGroups.Add(listenerGroup);
            var newGroup = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
            ActivateGroup(newGroup);
        }

        public static void DeactivateGroup(string listenerGroup) {
            if (!mSortedListeners.ContainsKey(listenerGroup))
                return;
            if (!mActiveGroups.Contains(listenerGroup))
                return;

            mActiveGroups.Remove(listenerGroup);
            var groupToDeactivate = (List<IInputListener<T>>)mSortedListeners[listenerGroup];
            DeactivateGroup(groupToDeactivate);
        }

        private static void DeactivateGroup(List<IInputListener<T>> group) {
            foreach (var listener in group)
                listener.Deactivate();
        }

        private static void ActivateGroup(List<IInputListener<T>> group) {
            foreach (var listener in group)
                listener.Activate();
        }

        /// <summary>
        /// Assumes the listener is already registered.
        /// </summary>
        private static bool IsListenerActive(IInputListener<T> listener) {
            string listenerGroup = listener.ListenerGroup;

            if (mCurrControllingGroup == listenerGroup)
                return true;

            return mActiveGroups.Contains(listenerGroup);
        }
    }
}