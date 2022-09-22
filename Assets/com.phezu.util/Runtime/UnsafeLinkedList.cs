﻿using System.Collections;
using UnityEngine;

namespace Phezu.Util
{
    /// <summary>
    /// Use this to do whatever you want every operation is allowed.
    /// </summary>
    public class UnsafeLinkedList<T> : IEnumerable
    {
        public UnsafeLinkedListNode First;
        public UnsafeLinkedListNode Last;

        public class UnsafeLinkedListIterator : IEnumerator {
            private UnsafeLinkedListNode first;
            private UnsafeLinkedListNode curr;
            public object Current => curr;

            public UnsafeLinkedListIterator(UnsafeLinkedListNode first) {
                this.first = first;
                curr = first;
            }

            public bool MoveNext() {
                if (curr.Next == null)
                    return false;

                curr = curr.Next;

                return true;
            }

            public void Reset() {
                curr = first;
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
                Last = First;
            }
            else if (First == Last)
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
            Last.Next = node;
            node.Prev = Last;

            while (node.Next != null && loopCheck > 0)
            {
                node = node.Next;
                loopCheck--;
            }

            if (loopCheck <= 0)
                Debug.LogError("You may be trying to loop an UnsafeLinkedList");

            Last = node;
        }

        public IEnumerator GetEnumerator() {
            return new UnsafeLinkedListIterator(First);
        }
    }
}