using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;
public class VoiceCommandHandler : MonoBehaviour
{
    [System.Serializable]
    public class Transformation
    {
        public string class_name;
        public int priority;
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class TransformationList
    {
        public List<Transformation> transformations;
    }

    public string serverUrl = "http://172.20.10.6:5000/transform"; // URL of your Flask server
    public GameObject modelParent = null;
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    void Start()
    {
        if (modelParent == null)
        {
            modelParent = new GameObject("ModelParent");
        }
        // Add the keyword and the action to execute when recognized
        keywords.Add("detect", OnDetectCommand);

        // Initialize and start the KeywordRecognizer
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (keywords.TryGetValue(args.text, out var action))
        {
            action.Invoke();
        }
    }

    private void OnDetectCommand()
    {
        // Call your function here
        Debug.Log("Detect command recognized!");
        StartCoroutine(GetTransformations());
    }

    IEnumerator GetTransformations()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl);
        Debug.Log("CreatedWebRequest");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch transformations: " + www.error);
        }
        else
        {
            // Parse JSON
            string jsonResponse = "{\"transformations\":" + www.downloadHandler.text + "}";
            TransformationList transformationList = JsonUtility.FromJson<TransformationList>(jsonResponse);
            StartCoroutine(VisualizeTransformations(transformationList.transformations));
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

    IEnumerator VisualizeTransformations(List<Transformation> transformations)
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


            yield return StartCoroutine(PlaySpatialAudio(obj, transformation.class_name));

        }
    }

    IEnumerator PlaySpatialAudio(GameObject obj, string className)
    {
        // Load audio clip (ensure audio files are in Resources folder or provide specific path)
        AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + className); // Example: Resources/Audio/className.wav
        if (audioClip == null)
        {
            Debug.LogWarning("Audio clip not found for " + className);
            yield break;
        }

        // Add AudioSource component to the object
        AudioSource audioSource = obj.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.spatialBlend = 1.0f; // Set to 1 for full 3D spatial audio
        audioSource.minDistance = 1f;    // Minimum distance for the audio to be heard
        audioSource.maxDistance = 10f;   // Maximum distance before the audio starts to fade
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Choose the rolloff mode

        // Play the audio
        audioSource.Play();

        // Wait until the audio has finished playing
        while (audioSource.isPlaying)
        {
            yield return null;
        }
    }

    void OnDestroy()
    {
        // Stop and dispose of the keyword recognizer when done
        keywordRecognizer.Stop();
        keywordRecognizer.Dispose();
    }
}

