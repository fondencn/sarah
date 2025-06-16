using Sarah.API.BusinessObjects;
using System;

namespace Sarah.API.Interfaces
{
    /// <summary>
    /// Public interface für die Sprachausgabe
    /// </summary>
    public interface ISpeechService
    {
        /// <summary>
        /// Initialisiert diesen Sprachdienst mit dem angegebenen Ort und den Aktivierungszuständen für Spracherkennung und -synthese.
        /// Diese Methode sollte nur einmal aufgerufen werden, um den Dienst zu konfigurieren.
        /// Wenn der Dienst bereits initialisiert ist, wird eine Ausnahme ausgelöst.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="isRecognitionEnabled"></param>
        /// <param name="isSynthesisEnabled"></param>
        void Initialize(string location, bool isRecognitionEnabled = true, bool isSynthesisEnabled = true);

        /// <summary>
        /// Ort, an dem sich dieser Sprachdienst befindet.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Gibt an, ob diese Sprachausgabe zur Zeit still geschaltet ist
        /// </summary>
        bool IsSilent { get; }

        /// <summary>
        /// Sagt was mit normaler Lautstärke
        /// </summary>
        /// <param name="text"></param>
        void Say(string text);

        /// <summary>
        /// Sagt was mit der angegebenen Lautstärke
        /// </summary>
        /// <param name="text"></param>
        /// <param name="vol"></param>
        void SayWithVolume(string text, SpeechVolume vol);

        /// <summary>
        /// Stellt diese Sprachausgabe auf nicht-mehr-stumm
        /// </summary>
        void DeactivateSilentTime();

        /// <summary>
        /// Deaktiviert diese Sprachausgabe bis zum angegebenen Zeitpunkt / Stummschaltung
        /// </summary>
        /// <param name="dateTime"></param>
        void ActivateSilentTime(DateTime dateTime);

        /// <summary>
        /// Startet die Wiedergabe einer Audiodatei
        /// </summary>
        void StartPlaySound(string audioFileName);

        /// <summary>
        /// Stoppt die Wiedergabe einer Audiodatei
        /// </summary>
        void StopPlaySound();
    }
}
