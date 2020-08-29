using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NUI
{
    public class NObjectPool<T> where T : class
    {
        private readonly Stack<T> _Stack = new Stack<T>();
        private readonly Func<T> _ActionOnGet;
        private readonly Action<T> _ActionOnRelease;

        public NObjectPool(Func<T> actionOnGet, Action<T> actionOnRelease)
        {
            _ActionOnGet = actionOnGet;
            _ActionOnRelease = actionOnRelease;
        }

        public T Get()
        {
            T element;
            if (_Stack.Count > 0)
            {
                element = _Stack.Pop();
            }
            else
            {
                if (_ActionOnGet != null)
                    element = _ActionOnGet();
                else
                    element = default(T);
            }

            return element;
        }

        public void Release(T element)
        {
            if (_ActionOnRelease != null)
                _ActionOnRelease(element);
            _Stack.Push(element);
        }
    }

    public class NGameObjectPool
    {
        private Transform RootPool;
        private Stack<GameObject> ObjStack = new Stack<GameObject>();

        public NGameObjectPool(GameObject root)
        {
            this.RootPool = root.transform;
        }

        //o(1)
        public void PushGameObject(GameObject go)
        {
            ObjStack.Push(go);
            go.SetActive(false);
            go.transform.SetParent(RootPool, false);
        }

        public GameObject PopGameObject()
        {
            if (ObjStack.Count > 0)
            {
                var go = ObjStack.Pop();
                go.SetActive(true);
            }
            return null;
        }
    }

    public static class NDictionaryPool<TKey, TValue>
    {
        private static readonly NObjectPool<Dictionary<TKey, TValue>> s_DictPool = new NObjectPool<Dictionary<TKey, TValue>>(null, l => l.Clear());

        public static Dictionary<TKey, TValue> Get()
        {
            return s_DictPool.Get();
        }

        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            s_DictPool.Release(toRelease);
        }
    }

    public static class NListPool<T>
    {
        private static readonly NObjectPool<List<T>> s_ListPool = new NObjectPool<List<T>>(() => new List<T>(), l => l.Clear());

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }

    public static class TextLinePool
    {
        private static readonly NObjectPool<NTextLine> LinePool = new NObjectPool<NTextLine>(() => new NTextLine(), (line) =>
        {
            line.Width = 0;
            line.Height = 0;
            line.OffsetX = 0;
            line.OffsetY = 0;
            line.BaseLine = 0;
            line.ParagraphIndent = 0;
            line.startCharIdx = 0;
            line.endCharIdx = 0;
        });

        public static NTextLine Get() { return LinePool.Get(); }
        public static void Release(NTextLine line) { LinePool.Release(line); }
    }

    public static class TextGlyphPool
    { 
        private static readonly NObjectPool<NTextGlyph> GlyphPool = new NObjectPool<NTextGlyph>(() => new NTextGlyph(), (glyph) =>
        {
            glyph.Char = '\0';
            glyph.MinX = 0;
            glyph.MinY = 0;
            glyph.Width = 0;
            glyph.Height = 0;
            glyph.Advance = 0;
            glyph.UnderlineColor = null;
            glyph.StrikethroughColor = null;
            glyph.SpriteIndex = -1;
            glyph.CustomCharTag = 0;
        });

        public static NTextGlyph Get() { return GlyphPool.Get(); }
        public static void Release(NTextGlyph glyph) { GlyphPool.Release(glyph); }
    }

    public static class TextElementPool
    {
        public static NObjectPool<NTextElement> ElementPool = new NObjectPool<NTextElement>(() => new NTextElement(), (NTextElement element) =>
        {
            element.Text = string.Empty;
            element.LinkParam = null;
            element.FontSize = 0;
            element.FontStyle = FontStyle.Normal;
            element.TopColor = element.BottomColor = Color.white;
            element.UnderlineColor = element.StrikethroughColor = null;
            element.SpriteIndex = 0;
            element.SpriteScale = 1f;
            element.SpriteAlign = NVerticalAlign.Bottom;
            element.AnimLength = 0;
            element.AnimFrame = 0;
            element.CustomCharTag = 0;
        });

        public static NTextElement Get() { return ElementPool.Get(); }
        public static void Release(NTextElement element) { ElementPool.Release(element); }
    }
}