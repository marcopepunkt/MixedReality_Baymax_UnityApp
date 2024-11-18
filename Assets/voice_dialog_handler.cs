using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;



public class voice_dialog_handler : MonoBehaviour
{
    [SerializeField]
    private AudioClip listenerActiveSound;   // Assign this in the Inspector

    [SerializeField]
    private AudioClip listenerInactiveSound; // Assign this in the Inspector

    [SerializeField]
    private float volume = 0.8f; // Just the volume of the sound
    
    private IO_Setup io_setup;
    private string IP;


    // Start is called before the first frame update
    void Start()
    {
        IO_Setup io_setup = GetComponent<IO_Setup>();

        if (io_setup == null)
        {
            Debug.LogError("IO_Setup not initialized!");
        }
        else
        {
            IP = io_setup.IP;
        }


    }


    public void MainHandler()
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
                    lowerCaseSpeech == "stop."  ||
                    lowerCaseSpeech == "end.")
                {
                    Debug.Log("Aborting dialog flow as per user request.");
                    PlaySound(listenerInactiveSound);
                    yield break; // Exit the coroutine
                }
                    // Handle the request with the input
                    var responseTextTask = SendToServerAndGetResponse(userSpeech);
                yield return new WaitUntil(() => responseTextTask.IsCompleted);
                string responseText = responseTextTask.Result;
                Debug.Log("Server response: " + responseText);
                // Play the response using TTS
                var playTextToSpeechTask = io_setup.PlayTextToSpeech(responseText);
                yield return new WaitUntil(() => playTextToSpeechTask.IsCompleted);
                PlaySound(listenerActiveSound);
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

    // This class is needed to transform the json to a class and then get the response
    class ServerResponse
    {
        public string response;
    }

    // This function sends the text to the server and gets the response
    private async Task<string> SendToServerAndGetResponse(string text)
    {

        string serverUrl =  IP + "/api"; // Replace with your server URL
        WWWForm form = new WWWForm();
        form.AddField("speechText", text);

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
                ServerResponse serverResponse = JsonUtility.FromJson<ServerResponse>(jsonResponse);
                return serverResponse.response; // Return only the "response" value
            }
            else
            {
                Debug.LogError("Error communicating with server: " + request.error);
                return null;
            }
        }
    }


    
    

}
