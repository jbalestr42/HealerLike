using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oisif.Editor
{
    // Writes what an object holds as "path = value" lines, whatever serializer stores it: the fields Unity or another
    // serializer would save (public ones, [SerializeField], [SerializeReference], or any attribute named in
    // serializedAttributeNames), read on the live object. Two snapshots of the same data give the same lines, so a
    // change of serializer can be checked by comparing them
    public class SnapshotWriter
    {
        // Attributes of other serializers marking a private field as saved, by type name (e.g. "OdinSerializeAttribute")
        public List<string> serializedAttributeNames = new List<string>();
        // Fields of these types are left out, by type name (e.g. the raw data of another serializer, "SerializationData")
        public List<string> ignoredTypeNames = new List<string>();
        // Names given to the Unity objects referenced, e.g. their asset path
        public Func<UnityEngine.Object, string> describeReference = obj => obj.name;
        public int maxDepth = 12;

        // Where the lines go, in the order of the fields
        readonly List<string> _lines = new List<string>();
        // Objects being written, against cycles
        readonly HashSet<object> _visiting = new HashSet<object>(ReferenceEqualityComparer.Instance);

        public List<string> Write(object root, string prefix = "")
        {
            _lines.Clear();
            _visiting.Clear();
            WriteFields(root, prefix, 0);
            return new List<string>(_lines);
        }

        void WriteFields(object obj, string path, int depth)
        {
            if (!_visiting.Add(obj))
            {
                _lines.Add($"{path} = <cycle>");
                return;
            }

            foreach (FieldInfo field in GetSerializedFields(obj.GetType()))
            {
                WriteValue(field.GetValue(obj), Join(path, field.Name), depth + 1);
            }
            _visiting.Remove(obj);
        }

        void WriteValue(object value, string path, int depth)
        {
            if (value == null || (value is UnityEngine.Object unityObject && unityObject == null))
            {
                _lines.Add($"{path} = null");
                return;
            }
            if (value is UnityEngine.Object reference)
            {
                _lines.Add($"{path} = &{describeReference(reference)} ({reference.GetType().Name})");
                return;
            }
            if (IsSimple(value.GetType()))
            {
                _lines.Add($"{path} = {FormatSimple(value)}");
                return;
            }
            if (depth > maxDepth)
            {
                _lines.Add($"{path} = <too deep>");
                return;
            }

            switch (value)
            {
                case IDictionary dictionary:
                    WriteDictionary(dictionary, path, depth);
                    break;
                case Array array when array.Rank > 1:
                    WriteMultiArray(array, path, depth);
                    break;
                case IList list:
                    _lines.Add($"{path}.Count = {list.Count}");
                    for (int i = 0; i < list.Count; i++)
                    {
                        WriteValue(list[i], $"{path}[{i}]", depth + 1);
                    }
                    break;
                default:
                    // The concrete type tells two polymorphic values apart
                    _lines.Add($"{path} : {value.GetType().Name}");
                    WriteFields(value, path, depth);
                    break;
            }
        }

        // Entries in the order of their keys: two dictionaries holding the same entries give the same lines
        void WriteDictionary(IDictionary dictionary, string path, int depth)
        {
            _lines.Add($"{path}.Count = {dictionary.Count}");
            List<(string key, object value)> entries = new List<(string, object)>();
            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add((FormatKey(entry.Key), entry.Value));
            }
            foreach ((string key, object value) in entries.OrderBy(entry => entry.key, StringComparer.Ordinal))
            {
                WriteValue(value, $"{path}[{key}]", depth + 1);
            }
        }

        void WriteMultiArray(Array array, string path, int depth)
        {
            int[] lengths = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
            _lines.Add($"{path}.Size = {string.Join("x", lengths)}");
            int[] indices = new int[array.Rank];
            for (int flat = 0; flat < array.Length; flat++)
            {
                int rest = flat;
                for (int dimension = array.Rank - 1; dimension >= 0; dimension--)
                {
                    indices[dimension] = rest % lengths[dimension];
                    rest /= lengths[dimension];
                }
                WriteValue(array.GetValue(indices), $"{path}[{string.Join(",", indices)}]", depth + 1);
            }
        }

        string FormatKey(object key)
        {
            if (key is UnityEngine.Object reference)
            {
                return reference != null ? describeReference(reference) : "null";
            }
            return key != null && IsSimple(key.GetType()) ? FormatSimple(key) : key?.ToString() ?? "null";
        }

        // Instance fields of the type and its base types, the base ones first, that a serializer would save
        public IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            List<Type> hierarchy = new List<Type>();
            for (Type current = type; current != null && current != typeof(object) && current != typeof(UnityEngine.Object); current = current.BaseType)
            {
                hierarchy.Insert(0, current);
            }

            foreach (Type current in hierarchy)
            {
                foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (IsSerialized(field) && !ignoredTypeNames.Contains(field.FieldType.Name))
                    {
                        yield return field;
                    }
                }
            }
        }

        bool IsSerialized(FieldInfo field)
        {
            if (field.IsNotSerialized || field.IsInitOnly || field.IsLiteral || field.Name.Contains("<"))
            {
                return false;
            }
            if (field.IsPublic)
            {
                return true;
            }
            return field.GetCustomAttributes(true).Any(attribute =>
                attribute is SerializeField || attribute is SerializeReference || serializedAttributeNames.Contains(attribute.GetType().Name));
        }

        static bool IsSimple(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Vector2Int)
                || type == typeof(Vector3Int) || type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Color32)
                || type == typeof(Rect) || type == typeof(Bounds) || type == typeof(LayerMask) || type == typeof(AnimationCurve)
                || type == typeof(Gradient);
        }

        static string FormatSimple(object value)
        {
            switch (value)
            {
                case float number:
                    return number.ToString("R", CultureInfo.InvariantCulture);
                case double number:
                    return number.ToString("R", CultureInfo.InvariantCulture);
                case string text:
                    return "\"" + text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
                // Every digit: the default text of Unity rounds to 2 decimals and would hide a change
                case Vector2 vector:
                    return Components(vector.x, vector.y);
                case Vector3 vector:
                    return Components(vector.x, vector.y, vector.z);
                case Vector4 vector:
                    return Components(vector.x, vector.y, vector.z, vector.w);
                case Quaternion rotation:
                    return Components(rotation.x, rotation.y, rotation.z, rotation.w);
                case Color color:
                    return Components(color.r, color.g, color.b, color.a);
                case Rect rect:
                    return Components(rect.x, rect.y, rect.width, rect.height);
                case Bounds bounds:
                    return Components(bounds.center.x, bounds.center.y, bounds.center.z, bounds.size.x, bounds.size.y, bounds.size.z);
                case LayerMask mask:
                    return mask.value.ToString(CultureInfo.InvariantCulture);
                case AnimationCurve curve:
                    return string.Join(";", curve.keys.Select(key => FormattableString.Invariant($"{key.time:R},{key.value:R},{key.inTangent:R},{key.outTangent:R}")));
                case Gradient gradient:
                    return string.Join(";", gradient.colorKeys.Select(key => FormattableString.Invariant($"{key.time:R}:{key.color}")))
                        + "|" + string.Join(";", gradient.alphaKeys.Select(key => FormattableString.Invariant($"{key.time:R}:{key.alpha:R}")));
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        static string Components(params float[] values)
        {
            return "(" + string.Join(", ", values.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + ")";
        }

        static string Join(string path, string name)
        {
            return string.IsNullOrEmpty(path) ? name : path + "." + name;
        }

        // Compares objects by reference, even the ones overriding Equals
        sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
