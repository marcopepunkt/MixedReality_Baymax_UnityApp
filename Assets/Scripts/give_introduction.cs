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
        await io_setup.PlayTextToSpeech("Hello, I am Baymax. I am here to assist you. Let me guide you through the application.");
        await io_setup.PlayTextToSpeech("To know what's in front of you, simply say: 'Hey Baymax!' After you hear the beep, you can ask questions about your surroundings. To end the conversation, just say: 'Abort!'");
        await io_setup.PlayTextToSpeech("If you already know the direction, say: 'Start!' and begin walking. You have to look down at a 45 degree angle and follow the sound. To end the obstacle avoidance, say : 'Stop!'");
        await io_setup.PlayTextToSpeech("To get a guidance on public transport just say 'Hey Baymax!' Wait for the beep and say for example: 'Take me to the main station!'. You can abort the guidance at any time saying: 'Abort!'.");
        await io_setup.PlayTextToSpeech("Thank you for your attention. Let's get started!");
    }

    public async void time_tell()
    {
        await io_setup.PlayTextToSpeech("The time is: " + System.DateTime.Now.ToString("h:mm tt"));
    }
}
