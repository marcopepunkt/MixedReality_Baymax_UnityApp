## These are the following Commands the user can input: 
- "Configure" - To open UI to change IP 
- "Start" - Start Obstacle Avoidance
- "Stop" - End Obstacle Avoidance
- "Hey Baymax" - To speak with baymax
- "Configure" / "Help" - Open introduction window

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
