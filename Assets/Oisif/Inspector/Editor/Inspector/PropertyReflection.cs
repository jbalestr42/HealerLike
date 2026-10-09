using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Oisif.Editor
{
    // What a serialized property is in the C# objects: the field it comes from, the object holding that field and
    // the value, found by following its path from the edited object
    public struct PropertyInfo
    {
        // Null for the parts with no field of their own (an element of a list, an entry of a dictionary)
        public FieldInfo field;
        // The object holding the field, or the list holding the element
        public object owner;
        public object value;
        // The field type, the element type for an element of a list
        public Type type;
    }

    public static class PropertyReflection
    {
        static readonly Regex ElementRegex = new Regex(@"^data\[(\d+)\]$");

        public const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static PropertyInfo Find(SerializedProperty property)
        {
            return Find(property.serializedObject.targetObject, property.propertyPath);
        }

        // Follows a property path ("data.items.Array.data[2].amount") from the root object
        public static PropertyInfo Find(object root, string path)
        {
            PropertyInfo info = new PropertyInfo { owner = null, value = root, type = root?.GetType() };
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part == "Array")
                {
                    continue;
                }

                Match element = ElementRegex.Match(part);
                if (element.Success)
                {
                    int index = int.Parse(element.Groups[1].Value);
                    Type elementType = GetElementType(info.type);
                    object list = info.value;
                    object value = list is IList items && index < items.Count ? items[index] : null;
                    info = new PropertyInfo { field = null, owner = list, value = value, type = value?.GetType() ?? elementType };
                    continue;
                }

                object owner = info.value;
                FieldInfo field = owner != null ? FindField(owner.GetType(), part) : (info.type != null ? FindField(info.type, part) : null);
                if (field == null)
                {
                    return new PropertyInfo();
                }
                object fieldValue = owner != null ? field.GetValue(owner) : null;
                info = new PropertyInfo { field = field, owner = owner, value = fieldValue, type = fieldValue?.GetType() ?? field.FieldType };
                // A list keeps its declared type, for the type of its elements
                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    info.type = field.FieldType;
                }
            }
            return info;
        }

        // The field of the type or of one of its base types
        public static FieldInfo FindField(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, InstanceMembers | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }

        public static Type GetElementType(Type listType)
        {
            if (listType == null)
            {
                return null;
            }
            if (listType.IsArray)
            {
                return listType.GetElementType();
            }
            foreach (Type implemented in listType.GetInterfaces())
            {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return implemented.GetGenericArguments()[0];
                }
            }
            return null;
        }

        // The value of a field, a property or a method without parameter of the object, or of one of its base
        // types; found false when there is no such member
        public static object GetMemberValue(object obj, string member, out bool found)
        {
            found = false;
            if (obj == null || string.IsNullOrEmpty(member))
            {
                return null;
            }

            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(member, InstanceMembers | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    found = true;
                    return field.GetValue(field.IsStatic ? null : obj);
                }
                System.Reflection.PropertyInfo property = type.GetProperty(member, InstanceMembers | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    found = true;
                    return property.GetValue(property.GetGetMethod(true).IsStatic ? null : obj);
                }
                MethodInfo method = type.GetMethod(member, InstanceMembers | BindingFlags.Static | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    found = true;
                    return method.Invoke(method.IsStatic ? null : obj, null);
                }
            }
            return null;
        }

        // True when the value counts as true: a true bool, an object, a number other than 0
        public static bool IsTruthy(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool flag:
                    return flag;
                case UnityEngine.Object unityObject:
                    return unityObject != null;
                case IConvertible convertible when value.GetType().IsPrimitive || value is Enum:
                    return Convert.ToDouble(convertible) != 0d;
                default:
                    return true;
            }
        }

        // The attribute on the field, or on the class of its value or of its type when inherited from classes
        public static T GetAttribute<T>(FieldInfo field) where T : Attribute
        {
            return field != null ? field.GetCustomAttribute<T>(true) : null;
        }
    }
}
