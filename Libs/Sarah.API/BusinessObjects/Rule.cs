using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sarah.API.BusinessObjects
{
    public class Rule
    {
        /// <summary>
        /// Name der Regel zur Anzeige an der Oberfläche
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ausführungsbedingung
        /// </summary>
        public RuleCondition? Condition { get; set; }

        /// <summary>
        /// Gibt an, wann diese Regel zuletzt zugetroffen ist (Nur In-Memory! wird außerdem verworfen, sobald die Daten neu aus der DB geladen werden)
        /// </summary>
        public DateTime? LastOccurence { get; set; }

        /// <summary>
        /// Auszuführende Aktion
        /// </summary>
        public RuleAction? Action { get; set; }

        public static Rule? Deserialize(byte[] value)
        {
            Rule? r = null;
            if (value?.Any() == true)
            {
                try
                {
                    string json = Encoding.UTF8.GetString(value);
                    r = JsonSerializer.Deserialize<Rule>(json);
                }
                catch
                {
                    // If JSON deserialization fails, return null
                    r = null;
                }
            }
            return r;
        }

        public byte[] Serialize()
        {
            string json = JsonSerializer.Serialize(this);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            return buffer;
        }
    }
}
