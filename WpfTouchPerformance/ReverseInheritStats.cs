using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfTouchPerformance {
    public static class ObjectExtensions {
        public static IEnumerable<T> Traverse<T>(this T start, Func<T, T> getSuccessor) where T : class {
            T current = start;
            while (current != null) {
                yield return current;
                current = getSuccessor(current);
            }
        }
    }

    public static class DictionaryExtension {
        public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : struct {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (key == null) return null;
            // ReSharper restore CompareNonConstrainedGenericWithNull
            if (dict.ContainsKey(key)) {
                return dict[key];
            }
            return null;
        }
    }

    public class Relationship {
        public DependencyObject Parent { get; set; }
        public DependencyObject Child { get; set; }
        public RelationshipType Type { get; set; }
        public int RecursionDepth { get; set; }
        protected bool Equals(Relationship other) {
            return Equals(Parent, other.Parent) && Equals(Child, other.Child) && Type == other.Type;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Relationship)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (Parent != null ? Parent.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Child != null ? Child.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
        }

        public override string ToString() {
            return $"{nameof(Parent)}: {Parent}, {nameof(Child)}: {Child}, {nameof(Type)}: {Type}";
        }
    }

    public enum RelationshipType {
        InputElementParent, LogicalParent
    }

    internal class ReverseInheritStats {

        /// <summary>
        /// Logs out various data on the child-parent relationships.
        ///
        /// </summary>
        /// <param name="sender"></param>
        public static void Log(object sender) {
            if (!(sender is DependencyObject)) return;
            var edgesOfGraph = new HashSet<Relationship>();
            var branchPoints = new List<DependencyObject>();
            Stopwatch startNew = Stopwatch.StartNew();
            var longestUnbranched = new List<List<Relationship>>();
            longestUnbranched.Add(new List<Relationship>());
            var timesEncountered = new Dictionary<DependencyObject, int>();
            bool success = Inspect(0, (DependencyObject)sender, branchPoints, edgesOfGraph, longestUnbranched, timesEncountered);

            TimeSpan startNewElapsed = startNew.Elapsed;
            startNew.Stop();

            var getParent = edgesOfGraph.ToLookup(r => r.Child);

            var branchPointsCounted = branchPoints
                .GroupBy(bp => bp, (key, group) => new { key, count = group.Count() })
                .ToDictionary(x => x.key, x => x.count);

            Func<DependencyObject, string> toString = dob => {
                if (dob == null) return "<null>";

                string result = $"{dob.GetType()}@{dob.GetHashCode()}[times({timesEncountered[dob]})]";
                result += branchPointsCounted.ContainsKey(dob)
                    ? $"[isBranchPoint({branchPointsCounted[dob]})]"
                    : "";
                return result;
            };
            Func<Relationship, string> relationshipToString = e => $"Parent: {toString(e.Parent)}, Child: {toString(e.Child)}, RelationshipType: {e.Type}, RecursionDepth: {e.RecursionDepth}";
            Console.WriteLine($"{sender}: branchPoints.Length={edgesOfGraph.Count}");
            Console.WriteLine($"Branchpoints: {Environment.NewLine}{string.Join(Environment.NewLine, branchPointsCounted.OrderBy(bpc => bpc.Value).Select(bp => toString(bp.Key)))}");
            Console.WriteLine($"Time taken {startNewElapsed}");
            string parentChain = string.Join(
                Environment.NewLine,
                (sender as DependencyObject).Traverse(o => getParent[o].FirstOrDefault()?.Parent).Select(o => toString(o)));
            Console.WriteLine("Parentchain:" + Environment.NewLine + parentChain);
            if (!success) {
                Console.WriteLine("Exceeded maximum recursion: ");
                string relationshipsList = string.Join(Environment.NewLine + Environment.NewLine,
                    longestUnbranched.Skip(longestUnbranched.Count - 100).Select(subList => "Count: " + subList.Count + Environment.NewLine + string.Join(Environment.NewLine, subList.Select(r => $"    {relationshipToString(r)}"))));
                Console.WriteLine(relationshipsList);
            }
            Console.WriteLine("Edges of graph: ");
            Console.WriteLine(
                string.Join(
                    Environment.NewLine,
                    edgesOfGraph.Select(e => relationshipToString(e))));
        }

        private static bool Inspect(int recursionDepth, DependencyObject element, List<DependencyObject> branchPoints, HashSet<Relationship> relationships, List<List<Relationship>> longestUnbranched, Dictionary<DependencyObject, int> timesEncountered) {
            if (longestUnbranched.Count > 30000) {
                return false;
            }
            int amount = timesEncountered.Get(element) ?? 0;
            amount++;
            timesEncountered[element] = amount;
            DependencyObject inputElementParent = GetInputElementParent(element);
            DependencyObject logicalParent = GetLogicalParent(element);
            if (inputElementParent != null) {
                Relationship relationship = new Relationship() { Parent = inputElementParent, Child = element, Type = RelationshipType.InputElementParent, RecursionDepth = recursionDepth };
                relationships.Add(relationship);
                longestUnbranched.Last().Add(relationship);
                bool res = Inspect(recursionDepth + 1, inputElementParent, branchPoints, relationships, longestUnbranched, timesEncountered);
                if (!res) {
                    return false;
                }
            }
            if (logicalParent == null || logicalParent == inputElementParent) {
                return true;
            }
            longestUnbranched.Add(new List<Relationship>());
            branchPoints.Add(element);
            Relationship item = new Relationship() { Parent = logicalParent, Child = element, Type = RelationshipType.LogicalParent, RecursionDepth = recursionDepth };
            relationships.Add(item);
            longestUnbranched.Last().Add(item);
            var res2 = Inspect(recursionDepth + 1, logicalParent, branchPoints, relationships, longestUnbranched, timesEncountered);
            if (!res2) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Inspired by .NET implementation.
        /// </summary>
        public static DependencyObject GetCoreParent(DependencyObject element) {
            DependencyObject dependencyObject = null;
            Visual visual = element as Visual;
            if (visual != null) {
                dependencyObject = VisualTreeHelper.GetParent(visual);
            } else {
                ContentElement reference = element as ContentElement;
                if (reference != null) {
                    dependencyObject = ContentOperations.GetParent(reference);
                } else {
                    Visual3D visual3D = element as Visual3D;
                    if (visual3D != null) {
                        dependencyObject = VisualTreeHelper.GetParent(visual3D);
                    }
                }
            }
            return dependencyObject;
        }

        /// <summary>
        /// Inspired by .NET implementation.
        /// </summary>
        public static DependencyObject GetInputElementParent(DependencyObject element) {
            DependencyObject dependencyObject = element;
            do {
                dependencyObject = GetCoreParent(dependencyObject);
            }
            while (dependencyObject != null && !IsValid(dependencyObject));
            return dependencyObject;
        }

        private static Dictionary<Type, MethodInfo> getUIParentCoreMethods = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// Inspired by .NET implementation.
        /// </summary>
        protected static DependencyObject GetUIParentCore(DependencyObject child) {
            MethodInfo methodInfo = FindGetUIParentCore(child);
            DependencyObject parent = methodInfo.Invoke(child, new object[0]) as DependencyObject;
            return parent;
        }

        private static MethodInfo FindGetUIParentCore(DependencyObject child) {
            if (getUIParentCoreMethods.ContainsKey(child.GetType())) {
                return getUIParentCoreMethods[child.GetType()];
            }
            MethodInfo findGetUIParentCore = child.GetType().GetMethod("GetUIParentCore", BindingFlags.NonPublic | BindingFlags.Instance);
            getUIParentCoreMethods.Add(child.GetType(), findGetUIParentCore);
            return findGetUIParentCore;
        }

        /// <summary>
        /// Inspired by .NET implementation.
        /// </summary>
        public static DependencyObject GetLogicalParent(DependencyObject element) {
            DependencyObject dependencyObject = null;
            UIElement uiElement = element as UIElement;
            if (uiElement != null) {
                dependencyObject = GetUIParentCore(uiElement);
            }
            ContentElement contentElement = element as ContentElement;
            if (contentElement != null) {
                dependencyObject = GetUIParentCore(contentElement);
            }
            return dependencyObject;
        }

        // Return whether the InputElement is one of our types.
        internal static bool IsValid(IInputElement e) {
            DependencyObject o = e as DependencyObject;
            return IsValid(o);
        }

        internal static bool IsValid(DependencyObject o) {
            return IsUIElement(o) || IsContentElement(o) || IsUIElement3D(o);
        }

        private static bool IsContentElement(DependencyObject dependencyObject) {
            return dependencyObject is ContentElement;
        }

        // Returns whether the given DynamicObject is a UIElement or not.
        internal static bool IsUIElement(DependencyObject o) {
            return o is UIElement;
        }

        // Returns whether the given DynamicObject is a UIElement3D or not.
        internal static bool IsUIElement3D(DependencyObject o) {
            return o is UIElement3D;
        }
    }
}