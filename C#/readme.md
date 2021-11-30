## Role of C# Component
* Cursor Manipulation of Main PC
  * Move the mouse cursor of the main PC using the touch information received from the Android Component (through TCP connection)  
* Handwriting (Gesture) recognition
  * The value of variable **touchedFingerIndex** (finger index (e.g., index or middle) that is touching the screen) is updated in real time.
  * If touchedFingerIndex == Middle Finger: Perform basic cursor manipulation of main PC
  * If touchedFingerIndex == Index Finger: Recognize the finger gesture stroke to correspond alphabet to trigger proper command.
