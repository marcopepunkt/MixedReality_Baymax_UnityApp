using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class give_introduction : MonoBehaviour
{
    public IO_Setup io_setup;

    // Start is called before the first frame update
    public async void introduce_the_app()
    {
        Debug.Log("Called Navigator.");
        await io_setup.PlayTextToSpeech("Hello, I am Baymax. I am here to assist you. Let me guide you through the application. To know what's in front of you, simply say: 'Hey Baymax!' After you hear the beep, you can ask questions about your surroundings. To end the conversation, just say: 'Abort!'");
        await io_setup.PlayTextToSpeech("If you already know the direction, say: 'Start!' and begin walking. If there's an object in your way, you'll hear a beep from the direction of the obstacle. To end the obstacle avoidance, say : 'End!'");
        await io_setup.PlayTextToSpeech("Need to know the time? Just ask: 'What's the time?' Want to change the voice or adjust other settings? Say: 'Configure!'");
        await io_setup.PlayTextToSpeech("Thank you for your attention. Let's get started!");
 
    }

    public async void time_tell()
    {
        await io_setup.PlayTextToSpeech("The time is: " + System.DateTime.Now.ToString("h:mm tt"));
    }
}
