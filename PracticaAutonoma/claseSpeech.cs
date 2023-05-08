/*
 *  Código realizado por: Daniel Bazo Correa y Clara Rubio Almagro
 *  Fecha: 01/05/2023
 *  Asignatura de Sistemas electrónicos interactivos
 *  ETSIT Universidad de Málaga
 *  
 *  Clase de control de audio
 */

// LIBRERIAS
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
            // es-ES
            // en-US

            if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return recognizer;
            }
        }

        return null;
    }
}
