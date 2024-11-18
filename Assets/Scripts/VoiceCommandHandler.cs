using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;
using Microsoft.CognitiveServices.Speech;
using UnityKeywordRecognizer = UnityEngine.Windows.Speech.KeywordRecognizer;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class VoiceCommandHandler : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI Visualization_IP = null;
    public Text Input_IP = null;


    [System.Serializable]
    public class Transformation
    {
        public string class_name;
        public int priority;
        public float x;
        public float y;
        public float z;
        public float depth;
        public string description;
    }

    [System.Serializable]
    public class TransformationList
    {
        public List<Transformation> transformations;
    }

    // TODO: change serverUrl for every PC/wifi
    public string serverUrl = "http://172.20.10.6:5000/transform"; // URL of your Flask server
    public GameObject modelParent = null;
    private UnityKeywordRecognizer keywordRecognizer;
    private Dictionary<string, Func<Task>> keywords = new Dictionary<string, Func<Task>>();

    // for TTS:
    // TODO: insert key, region for Azure TTS resource
    public readonly string azureKey = "None";
    private readonly string region = "switzerlandnorth";
    private SpeechSynthesizer synthesizer;

    void Start()
    {
        if (modelParent == null)
        {
            modelParent = new GameObject("ModelParent");
        }
        // Add the keyword and the action to execute when recognized
        keywords.Add("detect", OnDetectCommand);

        // Initialize and start the KeywordRecognizer
        keywordRecognizer = new UnityKeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();

        //initialize TTS
        var config = SpeechConfig.FromSubscription(azureKey, region);
        config.SpeechSynthesisLanguage = "en-US";
        config.SpeechSynthesisVoiceName = "en-US-AriaNeural"; 
        synthesizer = new SpeechSynthesizer(config);

        if (synthesizer == null)
        {
            Debug.LogError("Speech synthesizer not initialized!");
        }

        //Show IP Adress 
        Visualization_IP.text = serverUrl;
    }

    private async Task PlayTextToSpeech(string text)
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

    private async void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        try
        {
            if (keywords.TryGetValue(args.text, out var action))
            {
                await action.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during OnPhraseRecognized async action: {ex.Message}");
        }
    }
    public async void WrapperCallDetect()
    {
        await OnDetectCommand();
    }
    private async Task OnDetectCommand()
    {
        // Call your function here
        Debug.Log("Detect command recognized!");
        await GetTransformations();
    }

    // Helper method to wrap SendWebRequest in a Task
    private Task SendWebRequestAsync(UnityWebRequest www)
    {
        var tcs = new TaskCompletionSource<object>();

        // Register a callback for when the request is completed
        www.SendWebRequest().completed += (op) =>
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                tcs.SetResult(null); // Complete the task successfully
            }
            else
            {
                Debug.LogError("Request failed: " + www.error);
                tcs.SetException(new Exception(www.error)); // Set an exception if failed
            }
        };

        return tcs.Task;
    }

    private async Task GetTransformations()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl);
        Debug.Log("CreatedWebRequest");
        await SendWebRequestAsync(www);

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch transformations: " + www.error);
        }
        else
        {
            // Parse JSON
            string jsonResponse = "{\"transformations\":" + www.downloadHandler.text + "}";
            TransformationList transformationList = JsonUtility.FromJson<TransformationList>(jsonResponse);
            if (transformationList.transformations.Count == 0) // no objects were detected (neither azure nor our model), tell the user
            {
                await PlayTextToSpeech("No objects detected");
            }
            else if (transformationList.transformations[0].class_name == "none")  // we have only the description by AzureCV
            { 
                await PlayTextToSpeech(transformationList.transformations[0].description);
            }
            else
            {
                await PlayTextToSpeech(transformationList.transformations[0].description);
                await VisualizeTransformations(transformationList.transformations);
            }
        }
    }

    void ClearPreviousTransformations()
    {
        if (modelParent != null)
        {
            // Destroy all existing children of the parent object
            foreach (Transform child in modelParent.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private async Task VisualizeTransformations(List<Transformation> transformations)
    {
        foreach (var transformation in transformations)
        {
            // Create a primitive object (cube) to represent the detected object
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(modelParent.transform, false); // Ensure world position is retained


            obj.transform.position = new Vector3(transformation.x, transformation.y, transformation.z);

            // Assign color based on priority
            switch (transformation.priority)
            {
                case 1:
                    obj.GetComponent<Renderer>().material.color = Color.red; // Dangerous
                    break;
                case 2:
                    obj.GetComponent<Renderer>().material.color = Color.yellow; // Caution
                    break;
                case 3:
                    obj.GetComponent<Renderer>().material.color = Color.green; // Neutral
                    break;
            }

            // Create a new GameObject to hold the TextMesh
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform); // Set it as a child of the Cube
            textObj.transform.localPosition = new Vector3(0, 0.5f, 0); // Adjust position above the Cube

            // Add and configure TextMesh component
            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            if (textMesh == null)
            {
                Debug.LogError("Failed to add TextMesh component to the GameObject.");
                continue;
            }

            textMesh.text = transformation.class_name;
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 50;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;  // Ensures visibility

            obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            string textToPlay = transformation.class_name + " at " + string.Format("{0:F1}", transformation.depth) + " meters";
            await PlayTextToSpeech(textToPlay);

        }
    }


    public void CHANGEIP()
    {
        Visualization_IP.text = Input_IP.text;
        serverUrl = Input_IP.text;
    }

    void OnDestroy()
    {
        // Stop and dispose of the keyword recognizer when done
        keywordRecognizer.Stop();
        keywordRecognizer.Dispose();

        synthesizer.Dispose();
    }
}

