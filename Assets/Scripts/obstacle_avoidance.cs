using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;
using static obstacle_avoidance;




public class obstacle_avoidance : MonoBehaviour
{

    [SerializeField]
    public IO_Setup io_Setup;

    [SerializeField]
    private AudioClip heading_clip;
    
    [SerializeField]
    private AudioClip obstacle_clip;
    

    [SerializeField]
    public GameObject headGameObject;

    public GameObject ObstaclesParentObject;

    private GameObject headingObject;

    private bool running = false;
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
    }


    [System.Serializable]
    public class CombinedTransformationList
    {
        public List<Transformation> heading;
        public List<Transformation> obstacles;
    }



    public async void start_detection()
    {
        Transform globalTransform = ObstaclesParentObject.transform;
        Vector3 globalPosition = globalTransform.position;
        Quaternion globalRotation = globalTransform.rotation;
        Debug.Log($"Global Position: {globalPosition}, Global Rotation: {globalRotation.eulerAngles}");

        // Log the pose
        if (running) {
            Debug.Log("Obstacle avoidance already running");
            return;
        }
        running = true;
        await io_Setup.PlayTextToSpeech("Starting obstacle avoidance");
        Debug.Log("Starting the obstacle avoidance loop");


        
        Transform headTransform = headGameObject.transform;
        Vector3 headPosition = headTransform.position;
        Quaternion headRotation = headTransform.rotation;

        await InitHeading();


        while (running)
        {
            // Show the Head Pose for Debugging
            headPosition = headTransform.position;
            headRotation = headTransform.rotation;
            Debug.Log($"Head Position: {headPosition}, Head Rotation: {headRotation.eulerAngles}");
            await detect();
        }
    }

    public async void stop_detection()
    {
        running = false;
        ClearPreviousTransformations();
        PauseHeading();
        Destroy(headingObject);
        await io_Setup.PlayTextToSpeech("Stopping obstacle avoidance");
    }

    public async void calibrate()
    {
        serverUrl = io_Setup.IP + "/calibrate_detector";
        Debug.Log("CreatedWebReqest for Calibration");
        using (UnityWebRequest www = UnityWebRequest.Get(serverUrl))
        {
            // Send the request and await completion
            await SendWebRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to connect: " + www.error);
            }
            else
            {
                Debug.Log("Called Calibrator");
                Debug.Log(www.downloadHandler.text);
            }
        }
    }



    private async Task detect()
    {
        serverUrl = io_Setup.IP + "/collision";

        Debug.Log("Created WebRequest");
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
                if (www.responseCode == 204)
                {
                    Debug.Log("Received 204 No Content: No transformations to process.");
                    return; // Exit early as there's no data to process
                }
                ClearPreviousTransformations();

                // Parse JSON response
                string jsonResponse = www.downloadHandler.text;
                CombinedTransformationList combinedList = JsonUtility.FromJson<CombinedTransformationList>(jsonResponse);

                Debug.Log("Received transformations: " + jsonResponse);
                Debug.Log("Received " + combinedList.heading.Count + " heading transformations and " + combinedList.obstacles.Count + " obstacle transformations.");
                Debug.Log(combinedList.heading);
                Debug.Log(combinedList.obstacles);
                // Visualize the transformations
                if (combinedList.heading.Count == 0)
                {
                    PauseHeading();
                    await ShowCollisions(combinedList.obstacles, sound: true);
                }
                else
                {
                    if (combinedList.obstacles.Count == 0)
                    {
                        await UpdateHeading(combinedList.heading);
                    }
                    else
                    {
                        Debug.Log("Showing both heading and obstacles");
                        await ShowCollisions(combinedList.obstacles, sound: false);
                        await UpdateHeading(combinedList.heading);
                    }
                }
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


    private async Task ShowCollisions(List<Transformation> transformations, bool sound)
    {
        foreach (var transformation in transformations)
        {   
            Debug.Log("Showing obstacle at " + transformation.x + ", " + transformation.y + ", " + transformation.z);
            // Create a primitive object (cube) to represent the detected object
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(ObstaclesParentObject.transform); // Ensure world position is retained
            
            obj.transform.position = new Vector3(transformation.x, transformation.y, transformation.z);

            obj.GetComponent<Renderer>().material.color = Color.white; // Obstacle

            obj.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);

            if (sound)
            {
                await Play_Obstacle_Sound(obj);
            }
        }
    }

    private async Task UpdateHeading(List<Transformation> transformations)
    {
        Transformation heading_transform = transformations[0];
        headingObject.transform.position = new Vector3(heading_transform.x, heading_transform.y, heading_transform.z);
        // set active if iactive and play music if not playing
        // Check if the GameObject is inactive
        if (!headingObject.activeSelf)
        {
            headingObject.SetActive(true); // Activate the GameObject
        }

        AudioSource audioSource = headingObject.GetComponent<AudioSource>();

        // Check if the AudioSource is not playing
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play(); // Play the audio
        }
        
    }

    private async Task InitHeading()
    {
        // Create a primitive object (cube) to represent the detected object
        headingObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headingObject.SetActive(false);

        // Ensure world position is retained
        headingObject.transform.position = new Vector3(0f,0f,0f);
        headingObject.GetComponent<Renderer>().material.color = Color.blue; // Goal

        headingObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        

        AudioSource audioSource = headingObject.AddComponent<AudioSource>();
        audioSource.clip = heading_clip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f; // Full 3D audio
        audioSource.maxDistance = 10.0f; // Max distance for audio falloff
        audioSource.rolloffMode = AudioRolloffMode.Linear;



    }

    private async Task PauseHeading()
    {
        AudioSource audioSource = headingObject.GetComponent<AudioSource>();
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Deactivate the GameObject
        headingObject.SetActive(false);

    }

   
    private async Task Play_Obstacle_Sound(GameObject obj)
    {
        AudioSource audioSource = obj.AddComponent<AudioSource>();
        audioSource.clip = obstacle_clip;
        audioSource.volume = 1f; // Ensure volume is between 0 and 1
        audioSource.spatialBlend = 1.0f; // Full 3D audio
        audioSource.maxDistance = 10.0f; // Max distance for audio falloff
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.Play();

        // Ensure the audio is finished playing (in case of interruptions)
        while (audioSource.isPlaying)
        {
            await Task.Yield(); // Yield until playback completes
        }

    }

    void ClearPreviousTransformations()
    {
        if (ObstaclesParentObject != null)
        {
            // Destroy all existing children of the parent object
            foreach (Transform child in ObstaclesParentObject.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

}
