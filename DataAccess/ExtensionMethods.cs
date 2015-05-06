using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Musagetes.DataAccess
{
    public static class ExtensionMethods
    {
        public static bool IsEndElement(this XmlReader reader, string name = null)
        {
            if (reader.NodeType != XmlNodeType.EndElement)
                return false;
            return name == null || reader.Name.Equals(name);
        }

        public static void ConfirmElement(this XmlReader reader, string name)
        {
            if (!reader.LocalName.Equals(name))
                throw new FormatException("Invalid XML Format");
        }

        public static async Task<string> TryGetContentAsync(this XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                await reader.ReadAsync();
                return string.Empty;
            }
            var content = await reader.ReadElementContentAsStringAsync();
            return content;
        }

        public static int? FirstIndex<T>(this IList<T> list, 
            Func<T,bool> predicate)
        {
            for (var i = 0; i < list.Count; i++)
                if (predicate(list[i])) return i;
            return null;
        }
    }
}
