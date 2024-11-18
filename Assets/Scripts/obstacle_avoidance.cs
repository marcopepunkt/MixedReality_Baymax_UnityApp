using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;




public class obstacle_avoidance : MonoBehaviour
{

    [SerializeField]
    public IO_Setup io_Setup;

    [SerializeField]
    private AudioClip low_prio_object;
    
    [SerializeField]
    private AudioClip high_prio_object;
    
    [SerializeField]
    private AudioClip mid_prio_object;

    public GameObject modelParent = null;

    private bool running = true;
    private string serverUrl = null;

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

    public async void start_detection()
    {
        running = true;
        await io_Setup.PlayTextToSpeech("Starting obstacle avoidance");
        while (running)
        {
            Debug.Log("Detect command recognized!");
            await detect();

          
            

        }// Call your function here
        await io_Setup.PlayTextToSpeech("Ended obstacle avoidance");
    }

    public async void stop_detection()
    {
        running = false;
        
    }


    private async Task detect()
    {
        serverUrl = io_Setup.IP + "/transform";

        Debug.Log("CreatedWebRequest");
        using (UnityWebRequest www = UnityWebRequest.Get(serverUrl))
        {
            // Send the request and await completion
            await SendWebRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to fetch transformations: " + www.error);
            }
            else
            {
                // Parse JSON response
                string jsonResponse = "{\"transformations\":" + www.downloadHandler.text + "}";
                TransformationList transformationList = JsonUtility.FromJson<TransformationList>(jsonResponse);

                // Visualize the transformations
                await VisualizeTransformations(transformationList.transformations);

                // Clear any previous visualizations
                ClearPreviousTransformations();
            }
        }
    }

    // Helper method to await UnityWebRequest
    private async Task SendWebRequestAsync(UnityWebRequest www)
    {
        var asyncOp = www.SendWebRequest();

        while (!asyncOp.isDone)
        {
            await Task.Yield(); // Await until the request completes
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Request failed: {www.error}");
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
            await PlaySpatialAudioAsync(obj,transformation.priority);
        }
    }

    private async Task PlaySpatialAudioAsync(GameObject obj, int priorityLevel)
    {
        AudioClip audioClip = null;

        // Determine the correct audio clip based on priority level
        switch (priorityLevel)
        {
            case 0:
                audioClip = low_prio_object;
                break;
            case 1:
                audioClip = mid_prio_object;
                break;
            case 2:
                audioClip = high_prio_object;
                break;
        }

        if (audioClip != null)
        {
            PlayAudio(obj, audioClip);
            await Task.Delay(Mathf.CeilToInt(audioClip.length * 1000)); // Wait for audio to complete
        }
    }
    private void PlayAudio(GameObject obj, AudioClip audio)
    {
        if (audio != null)
        {
            AudioSource audioSource = obj.AddComponent<AudioSource>();
            audioSource.clip = audio;
            audioSource.volume = 1f; // Ensure volume is between 0 and 1
            audioSource.spatialBlend = 1.0f; // Full 3D audio
            audioSource.maxDistance = 10.0f; // Max distance for audio falloff
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();
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

}
