using System.Collections.Generic;
using UnityEngine;

namespace Phezu.Util
{
    /// <summary>
    /// Use this to do whatever you want every operation is allowed.
    /// </summary>
    public class UnsafeLinkedList<T> : IEnumerable<T>
    {
        public UnsafeLinkedListNode First;
        public UnsafeLinkedListNode Last;

        public T LastValue {
            get {
                if (First == null)
                    return default;
                if (Last == null)
                    return First.Value;
                return Last.Value;
            }
        }

        public class UnsafeLinkedListIterator : IEnumerator<T> {
            private UnsafeLinkedListNode first;
            private UnsafeLinkedListNode curr;

            T IEnumerator<T>.Current => curr.Value;

            public object Current => curr.Value;

            public UnsafeLinkedListIterator(UnsafeLinkedListNode first) {
                this.first = first;
                curr = null;
            }

            public bool MoveNext() {
                if (curr == null) {
                    curr = first;
                    return true;
                }

                if (curr.Next == null)
                    return false;

                curr = curr.Next;

                return true;
            }

            public void Reset() {
                curr = first;
            }

            public void Dispose() {
                
            }
        }

        public class UnsafeLinkedListNode
        {
            public T Value;
            public UnsafeLinkedListNode Next;
            public UnsafeLinkedListNode Prev;
        }

        public UnsafeLinkedList()
        {
            First = Last = null;
        }

        public void AddLast(T value)
        {
            if (First == null)
            {
                First = new();
                First.Value = value;
                First.Next = null;
                First.Prev = null;
                Last = null;
            }
            else if (Last == null)
            {
                Last = new();
                Last.Value = value;
                Last.Next = null;
                Last.Prev = First;
                First.Next = Last;
            }
            else
            {
                Last.Next = new();
                var secondLast = Last;
                Last = Last.Next;
                Last.Prev = secondLast;
                Last.Value = value;
                Last.Next = null;
            }
        }

        public void Connect(UnsafeLinkedListNode node, int loopCheck = 1000)
        {
            if (node == null) {
                Debug.Log("Trying to connect UnsafeLinkedList to a null node.");
                return;
            }

            if (Last == null) {
                First.Next = node;
                node.Prev = First;
            }
            else {
                Last.Next = node;
                node.Prev = Last;
            }

            while (node.Next != null && loopCheck > 0)
            {
                node = node.Next;
                loopCheck--;
            }

            if (loopCheck <= 0)
                Debug.LogError("You may be trying to loop an UnsafeLinkedList");

            Last = node;
        }

        public bool Remove(T value, IEqualityComparer<T> comparer = null) {
            var curr = First;

            while (curr != null) {
                if (comparer == null) {
                    if (curr.Value.Equals(value)) {
                        RemoveNode(curr);
                        return true;
                    }
                }
                else if (comparer.Equals(curr.Value, value)) {
                    RemoveNode(curr);
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        private void RemoveNode(UnsafeLinkedListNode node) {
            var prevNode = node.Prev;
            var nextNode = node.Next;

            node.Next = null;
            node.Prev = null;

            if (prevNode != null)
                prevNode.Next = nextNode;
            else
                First = nextNode;

            if (nextNode != null) {
                nextNode.Prev = prevNode;

                if (prevNode == null && nextNode.Next == null)
                    Last = null;

                return;
            }

            if (prevNode == null || (prevNode != null && prevNode.Prev == null))
                Last = null;
            else if (prevNode.Prev != null)
                Last = prevNode;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new UnsafeLinkedListIterator(First);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new UnsafeLinkedListIterator(First);
        }
    }
}