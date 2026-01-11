using Microsoft.CognitiveServices.Speech;
using System.IO;
using System.Reflection;

namespace Sarah.Voice.Recognition
{
    internal class ActivationKeyword
    {
        public string TextRepresentation { get; }
        public string ModelFileName { get; }

        public KeywordRecognitionModel Model { get; }

        public ActivationKeyword(string textRepresentation, string modelfile)
        {
            this.TextRepresentation = textRepresentation;
            this.ModelFileName = modelfile;

            FileInfo assemblyFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            FileInfo fi = new FileInfo(Path.Combine(assemblyFile.DirectoryName, "Resources", modelfile));
            if (!fi.Exists)
            {
                throw new InvalidDataException("Model-Datei " + fi.FullName + " nicht gefunden");
            }
            this.Model = KeywordRecognitionModel.FromFile(fi.FullName);
        }

    }
}
