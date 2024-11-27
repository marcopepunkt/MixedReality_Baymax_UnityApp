using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class IO_Setup : MonoBehaviour
{
    [SerializeField] 
    public TextMeshProUGUI Visualization_IP = null;
    public Text Input_IP = null;

    [SerializeField]
    public string IP = "http://127.0..1:5000"; // URL of your Flask server

    [SerializeField]
    public string azureKey = "None";

    private readonly string region = "switzerlandnorth";
    private SpeechSynthesizer synthesizer;
    private SpeechRecognizer recognizer;
    private SpeechConfig config;




    // Start is called before the first frame update
    void Start()
    {
        //initialize TTS and Recognizer
        config = SpeechConfig.FromSubscription(azureKey, region);
        config.SpeechSynthesisLanguage = "en-US";
        config.SpeechSynthesisVoiceName = "en-US-AriaNeural";
        synthesizer = new SpeechSynthesizer(config);
        recognizer = new SpeechRecognizer(config);

        if (synthesizer == null)
        {
            Debug.LogError("Speech synthesizer not initialized!");
        }

        if (recognizer == null)
        {
            Debug.LogError("Speech recognizer not initialized!");
        }

        Visualization_IP.text = IP;
    }

    public void CHANGEIP()
    {
        Visualization_IP.text = Input_IP.text;
        IP = Input_IP.text;
    }

    public void CHANGEVOICE()
    {
        if (config.SpeechSynthesisVoiceName == "en-US-AriaNeural")
        {
            config.SpeechSynthesisVoiceName = "en-GB-RyanNeural";
            synthesizer = new SpeechSynthesizer(config);

            Debug.Log("Voice changed to en-GB-RyanNeural");
        }
        else
        {
            config.SpeechSynthesisVoiceName = "en-US-AriaNeural";
            synthesizer = new SpeechSynthesizer(config);

            Debug.Log("Voice changed to en-US-AriaNeural");
        }
        PlayTextToSpeech("Hello there! This is my new voice. Nice to meet you.");
    }


    // This function plays the text using the TTS
    public async Task PlayTextToSpeech(string text)
    {
        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            Debug.Log("Speech synthesis succeeded!");
        }
        else
        {
            Debug.LogError($"Speech synthesis failed: {result.Reason}");
        }
    }

    // This function gets the recognized speech
    public async Task<string> GetRecognizedSpeechAsync()
    {
        if (recognizer == null)
        {
            Debug.LogError("Recognizer is not initialized.");
            return null;
        }

        Debug.Log("Starting recognition...");
        var speechRecognitionTask = recognizer.RecognizeOnceAsync(); // Recognizes a single input
        var result = await speechRecognitionTask; // Wait for the result

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Debug.Log($"Recognized: {result.Text}");
            return result.Text; // Return the recognized string
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Debug.Log("Speech could not be recognized.");
            return null;
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            Debug.LogError("Recognition canceled.");
            return null;
        }

        return null;
    }

    // I dont know what this function does
    private void OnDestroy()
    {
        if (recognizer != null)
        {
            recognizer.Dispose();
        }
    }



}
