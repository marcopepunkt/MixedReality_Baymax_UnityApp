using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;



public class dialog_handler : MonoBehaviour
{
    [SerializeField]
    private AudioClip listenerActiveSound;   // Assign this in the Inspector

    [SerializeField]
    private AudioClip listenerInactiveSound; // Assign this in the Inspector

    [SerializeField]
    private float volume = 0.8f; // Just the volume of the sound


    public dialog_google_maps dialog_google_maps;

    public IO_Setup io_setup;
    private string IP;

    private bool isDialogFlowRunning = false;


    public void StartDialogFlow()
    {
        if (isDialogFlowRunning)
        {
            Debug.LogWarning("Dialog flow is already running.");
            return;
        }

        isDialogFlowRunning = true;
        DialogFlow();
    }

    public async Task DialogFlow()
    {
        try
        {
            // Your DialogFlow logic here
            PlaySound(listenerActiveSound);
            while (true)
            {
                Debug.Log("_io_setup: " + io_setup);
                string userSpeech = await io_setup.GetRecognizedSpeechAsync();
                if (userSpeech != null)
                {
                    Debug.Log("User said: " + userSpeech);

                    string lowerCaseSpeech = userSpeech.ToLower().Trim();
                    if (lowerCaseSpeech.Contains("abort"))
                    {
                        Debug.Log("Aborting dialog flow as per user request.");
                        PlaySound(listenerInactiveSound);
                        break;
                    }

                    if (lowerCaseSpeech.Contains("take me to"))
                    {
                        Debug.Log("Initialized Google Maps navigation.");
                        await dialog_google_maps.Navigator(userSpeech);
                        break;
                    }
                    else
                    {
                        string responseText = await SendToServerAndGetResponse(userSpeech);
                        Debug.Log("Server response: " + responseText);

                        await io_setup.PlayTextToSpeech(responseText);
                        PlaySound(listenerActiveSound);
                    }
                }
                else
                {
                    Debug.Log("No speech recognized.");
                    PlaySound(listenerInactiveSound);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in DialogFlow: " + ex.Message);
        }
        finally
        {
            isDialogFlowRunning = false; // Reset the flag
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

        string serverUrl =  io_setup.IP + "/api"; // Replace with your server URL
        Debug.Log("serverUrl: " + serverUrl);
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
