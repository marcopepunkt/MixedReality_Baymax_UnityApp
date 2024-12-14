using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;

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
    public SpeechSynthesizer synthesizer;
    public SpeechRecognizer recognizer;
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


    private Coroutine ttsCoroutine; // Reference to the active coroutine
    private bool isPlaying = false; // Indicates if TTS is playing
    private bool isCanceled = false; // Indicates if TTS should be canceled

    public async Task PlayTextToSpeech(string text)
    {
        if (isPlaying)
        {
            Debug.Log("Speech synthesis is already running.");
            return;
        }

        isPlaying = true;
        isCanceled = false;

        try
        {
            var tcs = new TaskCompletionSource<bool>();

            // Start the coroutine and pass the TaskCompletionSource
            ttsCoroutine = StartCoroutine(SynthesizeSpeechCoroutine(text, tcs));

            // Await the completion of the coroutine
            await tcs.Task;

            if (isCanceled)
            {
                Debug.Log("Speech synthesis was canceled.");
            }
            else
            {
                Debug.Log("Speech synthesis completed successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during speech synthesis: {ex.Message}");
        }
        finally
        {
            isPlaying = false;
        }
    }

    private IEnumerator SynthesizeSpeechCoroutine(string text, TaskCompletionSource<bool> tcs)
    {
        var task = synthesizer.SpeakTextAsync(text);

        while (!task.IsCompleted)
        {
            if (isCanceled)
            {
                try
                {
                    // Stop the synthesizer if canceled
                    synthesizer.StopSpeakingAsync().Wait();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error stopping synthesizer: {ex.Message}");
                }

                tcs.SetResult(false); // Notify cancellation
                yield break; // Exit the coroutine
            }

            yield return null; // Wait for the next frame
        }

        // Handle task completion or failure outside the `while` loop
        if (task.Status == TaskStatus.RanToCompletion && task.Result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            tcs.SetResult(true); // Notify success
        }
        else if (task.IsFaulted)
        {
            Debug.LogError($"Speech synthesis failed: {task.Exception?.Message}");
            tcs.SetException(task.Exception); // Notify failure
        }
    }


    public void StopTextToSpeech()
    {
        if (ttsCoroutine != null)
        {
            isCanceled = true; // Set the cancellation flag

            Debug.Log("Speech synthesis stop requested.");
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
