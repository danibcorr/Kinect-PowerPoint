using Microsoft.Speech.Recognition;
using System;

public static class claseSpeech
{
    public static RecognizerInfo GetKinectRecognizer()
    {
        foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
        {
            string value;
            recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
            if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return recognizer;
            }
        }

        return null;
    }
}
