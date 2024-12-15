using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class dialog_google_maps : MonoBehaviour
{
    [SerializeField]
    private AudioClip listenerActiveSound;   // Assign this in the Inspector

    [SerializeField]
    private AudioClip listenerInactiveSound; // Assign this in the Inspector

    [SerializeField]
    private float volume = 0.8f; // Just the volume of the sound

    [SerializeField]
    public IO_Setup io_setup;

    private string IP;

    public async Task Navigator(string userSpeech)
    {
        var responseText = await GetNewDirectionsFromServer(userSpeech);
        string mainInstructions = responseText.main_instructions;
        Debug.Log("Server response: " + mainInstructions);

        // Play main instructions (e.g., go to tram stop, take tram, etc.)
        await io_setup.PlayTextToSpeech(mainInstructions);

        if (responseText.stop_coordinates == null)
        {
            Debug.Log("No stops found!");
            return;
        }

        List<Stop> stops = responseText.stop_coordinates;
        int currentStopIndex = 0;

        while (currentStopIndex < stops.Count)
        {
            await io_setup.PlayTextToSpeech("Would you like instructions to the next tram stop?");
            string userInput = (await io_setup.GetRecognizedSpeechAsync()).ToLower().Trim();

            if (userInput.Contains("yes"))
            {
                string currentStopCoords = stops[currentStopIndex].gps_coords;
                var subDirectionsResponse = await GetSubDirectionsFromServer(currentStopCoords);
                List<SubDirection> subDirections = subDirectionsResponse.subinstructions;

                int currentSubDirectionIndex = 0;
                while (currentSubDirectionIndex < subDirections.Count)
                {
                    string currentSubInstruction = subDirections[currentSubDirectionIndex].instruction;
                    await io_setup.PlayTextToSpeech(currentSubInstruction);

                    float distanceToTarget = 1000;
                    float threshold = 3;
                    float originalDistanceToTarget = subDirections[currentSubDirectionIndex].distance;

                    while (distanceToTarget > threshold)
                    {
                        var gpsComparison = await CompareGPSonServer(
                            subDirections[currentSubDirectionIndex].gps_lat,
                            subDirections[currentSubDirectionIndex].gps_lng
                        );

                        distanceToTarget = gpsComparison.distance_to_target;

                        if (distanceToTarget > (originalDistanceToTarget + 10.0))
                        {
                            await io_setup.PlayTextToSpeech("Looks like you went off the route. Let me give you new instructions.");
                            // TODO: Fetch and update new directions to the current waypoint.
                        }

                        string userSpeechStop = (await io_setup.GetRecognizedSpeechAsync()).ToLower().Trim();
                        if (userSpeechStop.Contains("break") || userSpeechStop.Contains("abort") || userSpeechStop.Contains("stop") || userSpeechStop.Contains("end"))
                        {
                            Debug.Log("Aborting dialog flow as per user request.");
                            PlaySound(listenerInactiveSound); // Add this if there's a relevant sound to play
                            return;
                        }
                    }

                    userSpeech = (await io_setup.GetRecognizedSpeechAsync()).ToLower().Trim();
                    if (userSpeech.Contains("break") || userSpeech.Contains("abort") || userSpeech.Contains("stop") || userSpeech.Contains("end"))
                    {
                        Debug.Log("Aborting dialog flow as per user request.");
                        PlaySound(listenerInactiveSound); // Add this if there's a relevant sound to play
                        return;
                    }

                    currentSubDirectionIndex++;
                }

                await io_setup.PlayTextToSpeech($"Great, you have arrived at {stops[currentStopIndex].name}!");

                if (currentStopIndex != stops.Count - 1)
                {
                    await io_setup.PlayTextToSpeech($"Your tram is at {stops[currentStopIndex].departure_time}.");
                }

                currentStopIndex++;
            }
            else return;
        }
    }


    private void PlaySound(AudioClip sound) // Play a sound 
    {
        if (sound != null)
        {
            GameObject tempAudio = new GameObject("TempAudio");
            tempAudio.transform.position = Camera.main.transform.position;

            // Add an AudioSource component and configure it
            AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
            audioSource.clip = sound;
            audioSource.volume = Mathf.Clamp01(volume); // Ensure volume is between 0 and 1
            audioSource.Play();

            // Destroy the temporary GameObject after the clip finishes playing
            Destroy(tempAudio, sound.length);

        }
    }

    // need this [System.Serializable] for json objects coming from server
    [System.Serializable]
    class Stop 
    {
        public string name;
        public string gps_coords;
        public string departure_time;
    }

    [System.Serializable]
    class MainDirectionsResponse
    {
        public string main_instructions;
        public List<Stop> stop_coordinates;
    }

    private async Task<MainDirectionsResponse> GetNewDirectionsFromServer(string text)
    {
        string serverUrl = io_setup.IP + "/directions"; // Replace with your server URL
        Debug.Log("serverUrl: " + serverUrl);

        // get destination address from user input
        string[] words = text.Split(' '); 
        int toIndex = Array.IndexOf(words, "to");
        string destination = string.Join(" ", words.Skip(toIndex + 1));
        Debug.Log("requesting directions to" + destination);

        WWWForm form = new WWWForm();
        form.AddField("speechText", destination);

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // Wait for the request to complete

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Speech sent successfully.");
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Full server response: " + jsonResponse);
                MainDirectionsResponse serverResponse = JsonUtility.FromJson<MainDirectionsResponse>(jsonResponse);
                return serverResponse; 
            }
            else
            {
                Debug.LogError("Error communicating with server: " + request.error);
                return null;
            }
        }
    }

    [System.Serializable]
    class SubDirection 
    {
        public string instruction;
        public float gps_lat;
        public float gps_lng;
        public float distance;
    }

    [System.Serializable]
    class SubDirectionsResponse
    {
        public List<SubDirection> subinstructions;
    }

    // input text is "{lat},{long}" gps coordinates of next tram stop or destination.
    // this function asks for walking subdirections to next tram stop, from current gps coordinates
    private async Task<SubDirectionsResponse> GetSubDirectionsFromServer(string destination)
    {

        string serverUrl = io_setup.IP + "/walking_directions"; // Replace with your server URL
        Debug.Log("serverUrl: " + serverUrl);
        WWWForm form = new WWWForm();
        form.AddField("speechText", destination);

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // Wait for the request to complete

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Speech sent successfully.");
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Full server response: " + jsonResponse);
                SubDirectionsResponse serverResponse = JsonUtility.FromJson<SubDirectionsResponse>(jsonResponse);
                return serverResponse;
            }
            else
            {
                Debug.LogError("Error communicating with server: " + request.error);
                return null;
            }
        }
    }

    [System.Serializable]
    class CompareGPSResponse
    {
        public float distance_to_target;
    }
    private async Task<CompareGPSResponse> CompareGPSonServer(float target_lat, float target_lng)
    {

        string serverUrl = io_setup.IP + "/compare_gps"; // Replace with your server URL
        Debug.Log("serverUrl: " + serverUrl);
        WWWForm form = new WWWForm();
        form.AddField("target_lat", target_lat.ToString());
        form.AddField("target_lng", target_lng.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // Wait for the request to complete

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Speech sent successfully.");
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Full server response: " + jsonResponse);
                CompareGPSResponse serverResponse = JsonUtility.FromJson<CompareGPSResponse>(jsonResponse);
                return serverResponse;
            }
            else
            {
                Debug.LogError("Error communicating with server: " + request.error);
                return null;
            }
        }
    }
}
