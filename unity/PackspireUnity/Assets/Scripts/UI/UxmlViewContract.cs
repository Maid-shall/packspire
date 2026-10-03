using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Fail-fast contract for named UXML insertion points. A broken template should
    /// be diagnosed at bind time instead of becoming a later NullReferenceException.
    /// </summary>
    public sealed class UxmlViewContract
    {
        private readonly string viewName;
        private readonly List<Entry> entries = new List<Entry>();

        private readonly struct Entry
        {
            public readonly string Name;
            public readonly Type Type;

            public Entry(string name, Type type)
            {
                Name = name;
                Type = type;
            }
        }

        public UxmlViewContract(string viewName)
        {
            this.viewName = string.IsNullOrWhiteSpace(viewName) ? "<unnamed view>" : viewName;
        }

        public UxmlViewContract Require<T>(string name) where T : VisualElement
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A required UXML element must have a name.", nameof(name));
            entries.Add(new Entry(name, typeof(T)));
            return this;
        }

        public void Validate(VisualElement root)
        {
            if (root == null)
                throw new InvalidOperationException($"UI template '{viewName}' has no root element.");

            List<string> failures = null;
            foreach (Entry entry in entries)
            {
                VisualElement element = root.Q(entry.Name);
                if (element != null && entry.Type.IsInstanceOfType(element)) continue;
                failures ??= new List<string>();
                string actual = element == null ? "missing" : element.GetType().Name;
                failures.Add($"{entry.Name} ({entry.Type.Name}, {actual})");
            }

            if (failures == null) return;
            throw new InvalidOperationException(
                $"UI template '{viewName}' failed its UXML contract: {string.Join(", ", failures)}.");
        }

        public T Get<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root?.Q<T>(name);
            if (element != null) return element;
            throw new InvalidOperationException(
                $"UI template '{viewName}' is missing required {typeof(T).Name} '{name}'.");
        }
    }
}
