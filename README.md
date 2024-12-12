## These are the following Commands the user can input: 
- "Configure" - To open UI to change IP 
- "Start" - Start Obstacle Avoidance
- "Stop" - End Obstacle Avoidance
- "Hey Baymax" - To speak with baymax
- "Toggle Profiler" - To toggle the performance thing

## All the scripts are within the Baymax_scripts (change IP and azure key here)
IO_Setup - Here all the IP Setting as well as Voice Recognition and TTS is handled
Obstacle_avoidance - Here all the Visualization and spatial Audio is handeled
Dialog_handler - Here the full dialog is handled.  

## API Design 
The Dialog is just a basic POST server and it just gets back a json like this; 
```json
{
    'response' : 'This is the response'
}
```


The Obstacle gets a list of jsons in the following format. 
```json
 {
            'class_name': class_name,
            'priority': priority,
            'x': random_x,
            'y': random_y,
            'z': random_z
}
```
The can be used to change the audio sound. The coordinates are used for the spatial Audio.


## How to run this stuff?
1. Run Flask server. Repo: Mixedreality_Baymax on branch unity_integration_azureOpenAI and run file scripts/baymax.py. To run Gemini you need to install the last 2 packages in environment.yaml
2. Set API key and IP in Baymax_Scripts -> IO_Setup (IP from the flask server) 
3. Build it as for hello cube 
4. Just run it. 

## What now? 
Havent checked about the accuracy of the positions of the boxes which are returned. 
Currently all Alert sounds are the same... 

## Google Maps

### set up gps connection between phone and pc:
1. install GPS2IP Lite app on phone [GPS2IP](https://www.google.com/url?sa=t&source=web&rct=j&opi=89978449&url=https://apps.apple.com/us/app/gps2ip-lite/id1562823492&ved=2ahUKEwjtvvLtoKKKAxXThv0HHX4zIIwQFnoECBcQAQ&usg=AOvVaw3MjoYW7jSYqW38cMqiVWUS).
2. inside the app: go to Settings > Connection Method, choose TCP Push
3. click on TCP Push and write the IP Address of your PC. The port number should be 11123 (should be the same on the app and on `receive_gps.py`)
4. in Settings > Network Selection, choose Cellular IP if you are connecting over your mobile data
5. on PC: download [Packet Sender](http://packetsender.com/)
6. inside the packet sender app; go to settings and enable TCP server. write down TCP server port (11123). instructions showing UI here under [Test that we can receive GPS2IP data](https://capsicumdreams.com/gps2ip/tcpPushMode.php). on the phone app, enable GPS2IP Lite on top of the main screen and follow the instructions from last url to check you are receiving packets on packet sender.
7. now you don't have to run packet sender app again when running baymax.py. just enable GPSIP Lite on the phone app each time you need gps coordinates.

### make requests to google maps while runnning the app:
1. say "hey maps"
2. say "take me to ... " -> provides instructions like tram line, time, departure stop
3. after hearing "Would you like additional instructions to first tram stop?", if you say "yes", it will give you the first walking instruction to the first tram stop and start receiving your gps coordinates from the phone. when you get past the gps coordinates of the first instruction, should tell you the next instruction (need to test this first :D)

ps: there might be problems with saying "stop","break" in google maps mode

