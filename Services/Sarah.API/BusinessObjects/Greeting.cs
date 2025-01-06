using System;

namespace Sarah.Voice
{
    /// <summary>
    /// Bgrüßungsformeln
    /// </summary>
    public static class Greeting
    {
        /// <summary>
        /// Die für die aktuelle Uhrzeit passende Grußformel
        /// </summary>
        public static string Current
        {
            get
            {
                string greeting;
                if (DateTime.Now.Hour <= 6)
                {
                    greeting = "Gute Nacht";
                }
                if (DateTime.Now.Hour <= 9)
                {
                    greeting = "Guten Morgen";
                }
                else if (DateTime.Now.Hour < 12)
                {
                    greeting = "Guten Tag";
                }
                else if (DateTime.Now.Hour < 14)
                {
                    greeting = "Guten Mittag";
                }
                else if (DateTime.Now.Hour < 16)
                {
                    greeting = "Guten Nachmittag";
                }
                else if (DateTime.Now.Hour < 22)
                {
                    greeting = "Guten Abend";
                }
                else
                {
                    greeting = "Gute Nacht";
                }

                greeting = AppendSpecialDayGreeting(greeting);

                return greeting;
            }
        }

        private static string AppendSpecialDayGreeting(string greeting)
        {
            DateTime today = DateTime.Today;
            if(today.Month == 12 && today.Day == 24)
            {
                greeting += " und fröhliche Weihnachten";
            }
            else if (today.Month == 1 && today.Day == 1)
            {
                greeting += " und ein schönes neues Jahr";
            }
            else if (today.Month == 6 && today.Day == 8)
            {
                greeting += " und herzlichen Glückwunsch zum Geburtstag";
            }
            else if (today.Month == 1 && today.Day == 14)
            {
                greeting += " und herzlichen Glückwunsch zum Geburtstag";
            }
            else if (today.Month == 2 && today.Day == 14)
            {
                greeting += " und  alles Gute zu Valentinstag";
            }
            return greeting;
        }
    }
}
