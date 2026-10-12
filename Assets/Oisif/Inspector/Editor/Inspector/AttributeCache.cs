using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oisif.Editor
{
    // The attributes of the fields, types and methods, read once: the inspector asks for them at every event, for
    // every property, and reading them by reflection each time slows the long inspectors down. Inherited ones
    // included, like MemberInfo.GetCustomAttributes(type, true)
    public static class AttributeCache
    {
        // T[] by member and T
        static readonly Dictionary<(MemberInfo, Type), Array> AttributesByMember = new Dictionary<(MemberInfo, Type), Array>();

        // Every attribute T of the member, none for a null member
        public static T[] GetAll<T>(MemberInfo member) where T : Attribute
        {
            if (member == null)
            {
                return Array.Empty<T>();
            }
            if (!AttributesByMember.TryGetValue((member, typeof(T)), out Array attributes))
            {
                attributes = Array.ConvertAll(member.GetCustomAttributes(typeof(T), true), attribute => (T)attribute);
                AttributesByMember[(member, typeof(T))] = attributes;
            }
            return (T[])attributes;
        }

        // The first attribute T of the member, null when it has none
        public static T Get<T>(MemberInfo member) where T : Attribute
        {
            T[] attributes = GetAll<T>(member);
            return attributes.Length > 0 ? attributes[0] : null;
        }

        public static bool Has<T>(MemberInfo member) where T : Attribute
        {
            return GetAll<T>(member).Length > 0;
        }
    }
}
