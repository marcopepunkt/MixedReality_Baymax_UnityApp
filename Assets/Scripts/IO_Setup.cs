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
    //public string IP = "http://127.0..1:5000"; // URL of your Flask server
    public string IP = "http://127.0.0.1:5000";

    [SerializeField]
    public string azureKey = "None";

    private readonly string region = "switzerlandnorth";
    public SpeechSynthesizer synthesizer;
    public SpeechRecognizer recognizer;
    private SpeechConfig config;

    [Header("Speech Visualization")]
    [SerializeField, Tooltip("Text component to display speech-to-text output")]
    private TextMeshProUGUI speechToTextDisplay;
    [SerializeField, Tooltip("Text component to display text-to-speech input")]
    private TextMeshProUGUI textToSpeechDisplay;
    [SerializeField, Tooltip("Panel containing speech-to-text elements")]
    private GameObject speechToTextPanel;
    [SerializeField, Tooltip("Panel containing text-to-speech elements")]
    private GameObject textToSpeechPanel;

    [Header("UI Positioning")]
    [SerializeField, Tooltip("Distance from the camera (in meters)")]
    private float distanceFromCamera = 2f;
    [SerializeField, Tooltip("Vertical offset from camera center (in meters)")]
    private float verticalOffset = -0.2f;

    [Header("Visual Settings")]
    [SerializeField]
    private int maxHistoryLines = 5;
    [SerializeField]
    private float textDisplayDuration = 3.0f;

    private string speechToTextHistory = "";
    private float lastUpdateTime;
    private Camera mainCamera;
    private Transform canvasTransform;
    private Coroutine ttsCoroutine;
    private bool isPlaying = false;
    private bool isCanceled = false;



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
        // Initialize visualization
        mainCamera = Camera.main;
        canvasTransform = GetComponentInParent<Canvas>()?.transform;
        if (speechToTextDisplay != null)
        {
            speechToTextDisplay.text = "";
        }

        if (textToSpeechDisplay != null)
        {
            textToSpeechDisplay.text = "";
        }
        Visualization_IP.text = IP;

    }

    private void LateUpdate()
    {
        UpdateCanvasPosition();

        // Auto-hide panels after duration
        if (Time.time - lastUpdateTime > textDisplayDuration)
        {
            if (speechToTextPanel != null && !isPlaying)
            {
                speechToTextPanel.SetActive(false);
            }

            if (textToSpeechPanel != null && !isPlaying)
            {
                textToSpeechPanel.SetActive(false);
            }
        }
    }

    private void UpdateCanvasPosition()
    {
        if (mainCamera != null && canvasTransform != null)
        {
            Vector3 position = mainCamera.transform.position +
                             mainCamera.transform.forward * distanceFromCamera +
                             mainCamera.transform.up * verticalOffset;

            canvasTransform.position = position;
            canvasTransform.rotation = Quaternion.LookRotation(
                canvasTransform.position - mainCamera.transform.position);
        }
    }

    // Visualization update methods
    public void UpdateSpeechToTextDisplay(string recognizedText)
    {
        if (speechToTextDisplay != null)
        {
            speechToTextHistory = AddToHistory(speechToTextHistory, recognizedText);
            speechToTextDisplay.text = speechToTextHistory;
            lastUpdateTime = Time.time;

            if (speechToTextPanel != null)
            {
                speechToTextPanel.SetActive(true);
            }
        }
    }

    public void UpdateTextToSpeechDisplay(string textToSpeak)
    {
        if (textToSpeechDisplay != null)
        {
            textToSpeechDisplay.text = textToSpeak;
            lastUpdateTime = Time.time;

            if (textToSpeechPanel != null)
            {
                textToSpeechPanel.SetActive(true);
            }
        }
    }

    private string AddToHistory(string history, string newText)
    {
        string updatedHistory = string.IsNullOrEmpty(history) ? newText : history + "\n" + newText;
        string[] lines = updatedHistory.Split('\n');
        if (lines.Length > maxHistoryLines)
        {
            updatedHistory = string.Join("\n", lines, lines.Length - maxHistoryLines, maxHistoryLines);
        }
        return updatedHistory;
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




    public async Task PlayTextToSpeech(string text)
    {
        if (isPlaying)
        {
            Debug.Log("Speech synthesis is already running.");
            return;
        }
        UpdateTextToSpeechDisplay(text);
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
            UpdateSpeechToTextDisplay(result.Text);
            return result.Text; // Return the recognized string
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Debug.Log("Speech could not be recognized.");
            UpdateSpeechToTextDisplay("Speech could not be recognized.");
            return null;
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            Debug.LogError("Recognition canceled.");
            UpdateSpeechToTextDisplay("Recognition canceled.");
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
