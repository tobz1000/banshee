//
// ListModel.cs
//
// Author:
//       Mike Urbanski <michael.c.urbanski@gmail.com>
//
// Copyright (c) 2009 Michael C. Urbanski
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;

using Migo2.Collections;

using Hyena.Data;
using Hyena.Collections;

using Hyena;
using Banshee.Base;
using Banshee.Paas.Utils;

namespace Banshee.Paas.Data
{
    public class ListModel<T> : IListModel<T>
    {
        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        private List<T> items;
        private Selection selection;

        public virtual bool CanReorder {
            get { return false; }
        }

        public virtual int Count {
            get { return items.Count; }
        }

        public Selection Selection {
            get { return selection; }
        }

        public T this[int index] {
            get { return GetIndex (index); }
        }

        protected List<T> Items {
            get { return items; }
        }

        public ListModel () : this (null)
        {
        }

        public ListModel (Selection selection)
        {
            items = new List<T> ();
            this.selection = selection ?? new Selection ();
        }

        public void Add (T item)
        {
            if (!item.Equals (default (T))) {
                items.Add (item);
                OnReloaded ();
            }
        }

        public void Add (IEnumerable<T> items)
        {
            if (items != null) {
                foreach (T item in items) {
                    if (item != null) {
                        this.items.Add (item);
                    }
                }

                OnReloaded ();
            }
        }

        public IEnumerable<T> GetSelected ()
        {
            T item = default (T);
            List<T> selected = new List<T> ();

            foreach (int i in selection) {
                item = GetIndex (i);

                if (item != null) {
                    selected.Add (item);
                }
            }

            return selected;
        }

        public void Remove (T item)
        {
            if (item != null) {
                items.Remove (item);
                OnReloaded ();
            }
        }

        public void Remove (IEnumerable<T> items)
        {
            if (items != null) {
                foreach (T item in items) {
                    if (item != null) {
                        this.items.Remove (item);
                    }
                }

                OnReloaded ();
            }
        }

        public void Clear ()
        {
            items.Clear ();
            OnCleared ();
        }

        public void Reload ()
        {
            OnReloaded ();
        }

        public void Reorder (int[] newWorldOrder)
        {
            int len = newWorldOrder.Length;
            int[] order = new int[len];
            Dictionary<T, int> positions = new Dictionary<T, int> (len);

            int i = 0;
            for (; i < order.Length; ++i) {
                order[newWorldOrder[i]] = i;
            }

            i = 0;
            foreach (var t in items) {
                positions.Add (t, order[i++]);
            }

            items.Sort (new OrderComparer<T> (positions));
            selection.Clear ();

            OnReloaded ();
        }

        public object GetItem(int index)
        {
            return GetIndex(index);
        }

        private T GetIndex (int index)
        {
            if (index >= 0 && index < items.Count) {
                return items[index];
            }

            return default (T);
        }

        protected virtual void OnReloaded ()
        {
            ThreadAssist.ProxyToMain (delegate {
                EventHandler handler = Reloaded;

                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            });
        }

        protected virtual void OnCleared ()
        {
            ThreadAssist.ProxyToMain (delegate {
                EventHandler handler = Cleared;

                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            });
        }
    }
}