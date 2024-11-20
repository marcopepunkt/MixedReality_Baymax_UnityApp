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
1. Run Flask server (Repo: Mixedreality_Baymax on branch hl2ss and run file PC_to_hololens_python_project/send_to_hololens.py)
2. Set API key and IP in Baymax_Scripts -> IO_Setup (IP from the flask server) 
3. Build it as for hello cube 
4. Just run it. 

## What now? 
Havent checked about the accuracy of the positions of the boxes which are returned. 
Currently all Alert sounds are the same... 
