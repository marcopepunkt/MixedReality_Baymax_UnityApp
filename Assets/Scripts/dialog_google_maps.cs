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
    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("Starting dialog flow...");
        StartCoroutine(DialogFlow());
    }

    private IEnumerator DialogFlow()
    {
        // Play sound to show listener is active
        PlaySound(listenerActiveSound);
        while (true)
        {
            Debug.Log("_io_setup: " + io_setup);
            // Simulate speech-to-text process (replace with actual Speech to Text logic)
            var userSpeechTask = io_setup.GetRecognizedSpeechAsync();
            yield return new WaitUntil(() => userSpeechTask.IsCompleted);
            string userSpeech = userSpeechTask.Result;

            if (userSpeech != null)
            {
                Debug.Log("User said: " + userSpeech);

                // Check if the user wants to abort the flow (exact match)
                string lowerCaseSpeech = userSpeech.ToLower().Trim(); // Normalize the input
                if (lowerCaseSpeech == "break." ||
                    lowerCaseSpeech == "abort." ||
                    lowerCaseSpeech == "stop." ||
                    lowerCaseSpeech == "end.")
                {
                    Debug.Log("Aborting dialog flow as per user request.");
                    PlaySound(listenerInactiveSound);
                    yield break; // Exit the coroutine
                }
                if (lowerCaseSpeech.Contains("take me to")) {
                    var responseTextTask = GetNewDirectionsFromServer(userSpeech);
                    yield return new WaitUntil(() => responseTextTask.IsCompleted);
                    string mainInstructions = responseTextTask.Result.main_instructions;
                    Debug.Log("Server response: " + mainInstructions);

                    // Play main instructions (go to tram stop, take tram at ..., ...)
                    var playTextToSpeechTask = io_setup.PlayTextToSpeech(mainInstructions);
                    yield return new WaitUntil(() => playTextToSpeechTask.IsCompleted);

                    if (responseTextTask.Result?.stop_coordinates == null)
                    {
                        Debug.Log("No stops found!");
                        yield break;
                    }

                    List<Stop> stops = responseTextTask.Result.stop_coordinates;
                    int currentStopIndex = 0;

                    while (currentStopIndex < stops.Count) {
                        // go over each tram stop, and play subinstructions for walking to the tram stop
                        var playQuestionTask = io_setup.PlayTextToSpeech("Would you like instructions to the next tram stop?");
                        yield return new WaitUntil(() => playQuestionTask.IsCompleted);

                        var userInputTask = io_setup.GetRecognizedSpeechAsync();
                        yield return new WaitUntil(() => userInputTask.IsCompleted);
                        string userInput = userInputTask.Result;
                        userInput = userInput.ToLower().Trim();

                        if (userInput.Contains("yes"))
                        {
                            // calculate subdirections to next tram stop
                            string currentStopCoords = stops[currentStopIndex].gps_coords;
                            var serverResponseTask = GetSubDirectionsFromServer(currentStopCoords);
                            yield return new WaitUntil(() => serverResponseTask.IsCompleted);
                            List<SubDirection> subDirections = serverResponseTask.Result.subinstructions;

                            int currentSubDirectionIndex = 0;
                            while (currentSubDirectionIndex < subDirections.Count)
                            {
                                // go over each subdirection (to the tramstop). a subdirection can be: "Walk straight for 10 meters."
                                string currentSubInstruction = subDirections[currentSubDirectionIndex].instruction;
                                var playSubInstructionTask = io_setup.PlayTextToSpeech(currentSubInstruction);
                                yield return new WaitUntil(() => playSubInstructionTask.IsCompleted);

                                // the current distance in meters to the next waypoint. set it to a large number so the while loop starts
                                float distanceToTarget = 1000;
                                float threshold = 3;
                                float originalDistanceToTarget = subDirections[currentSubDirectionIndex].distance;
                                // compare current GPS coordinates to target GPS coordinates (of the subinstruction), on server
                                // only if we reach target GPS coordinates, we play the next subinstruction
                                while (distanceToTarget > threshold)
                                {
                                    var CompareGPSTask = CompareGPSonServer(subDirections[currentSubDirectionIndex].gps_lat, subDirections[currentSubDirectionIndex].gps_lng);
                                    yield return new WaitUntil(() => CompareGPSTask.IsCompleted);

                                    distanceToTarget = CompareGPSTask.Result.distance_to_target;

                                    // if current distance to target is more than 10 meters to the original estimated distance, warn the user
                                    if (distanceToTarget > (originalDistanceToTarget + 10.0))
                                    {
                                        var playFeedbackTask = io_setup.PlayTextToSpeech("Looks like you went off the route. Let me give you new instructions.");
                                        yield return new WaitUntil(() => playFeedbackTask.IsCompleted);

                                        //TODO: get new directions to the next waypoint subDirections[currentSubDirectionIndex].gps_lat and gps_lng
                                    }

                                    // TODO: when user says stop/break/quit, stop. below doesn't work :((
                                    var userSpeechStopTask = io_setup.GetRecognizedSpeechAsync();
                                    yield return new WaitUntil(() => userSpeechStopTask.IsCompleted);
                                    string userSpeechStop = userSpeechStopTask.Result;
                                    userSpeechStop = userSpeechStop.ToLower().Trim(); // Normalize the input
                                    if (lowerCaseSpeech == "break." ||
                                        lowerCaseSpeech == "abort." ||
                                        lowerCaseSpeech == "stop." ||
                                        lowerCaseSpeech == "end.")
                                    {
                                        Debug.Log("Aborting dialog flow as per user request.");
                                        PlaySound(listenerInactiveSound);
                                        yield break; // Exit the coroutine
                                    }
                                }
                                // we have reached next target/waypoint on the way to the tram stop
                                currentSubDirectionIndex++;
                            }

                            var playTramStopTask = io_setup.PlayTextToSpeech("Great, you have arrived to " + stops[currentStopIndex].name + "!");
                            yield return new WaitUntil(() => playTramStopTask.IsCompleted);

                            // if we are at a tram stop, say also what time the tram is coming
                            // TODO: transform time into minutes
                            // TODO: calculate next tram if we missed the original tram?
                            if (currentStopIndex != stops.Count - 1)
                            {
                                var playTramTimeTask = io_setup.PlayTextToSpeech("Your tram is at " + stops[currentStopIndex].departure_time + ".");
                                yield return new WaitUntil(() => playTramTimeTask.IsCompleted);
                            }

                            // go to next tram stop / destination
                            ++currentStopIndex;
                            // TODO: if user says stop, break, etc

                        }
                        else{
                            Debug.Log("Aborting dialog flow as per user request.");
                            PlaySound(listenerInactiveSound);
                            yield break; // Exit the coroutine
                        }

                    }

                }
                if (lowerCaseSpeech.Contains("repeat")) {
                    //TODO
                }
            }
            else
            {
                Debug.Log("No speech recognized.");
                // Play sound indicating speech input is complete
                PlaySound(listenerInactiveSound);
                break;
            }
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
