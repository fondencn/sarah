using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    public class Rule
    {
        /// <summary>
        /// Name der Regel zur Anzeige an der Oberfläche
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Ausführungsbedingung
        /// </summary>
        public RuleCondition Condition { get; set; }

        /// <summary>
        /// Gibt an, wann diese Regel zuletzt zugetroffen ist (Nur In-Memory! wird außerdem verworfen, sobald die Daten neu aus der DB geladen werden)
        /// </summary>
        public DateTime? LastOccurence { get; set; }

        /// <summary>
        /// Auszuführende Aktion
        /// </summary>
        public RuleAction Action { get; set; }

        public static Rule Deserialize(byte[] value)
        {
            Rule r = null;
            if (value?.Any() == true)
            {
                BinaryFormatter serializer = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream(value))
                {
                    r = (Rule)serializer.Deserialize(ms);
                }
            }
            return r;
        }

        public byte[] Serialize()
        {
            byte[] buffer;
            BinaryFormatter serializer = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.Serialize(ms, this);
                ms.Flush();
                buffer = ms.ToArray();
            }
            return buffer;
        }
    }
}
