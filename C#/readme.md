## C# side
C#에서 수행되는 기능은 다음과 같다.

* **Main PC의 Cursor Manipulation**
  * Android side에서 TCP 통신으로 넘어온 터치 좌표를 이용해 Main PC의 마우스 커서를 움직인다.
* **Gesture Recognition**
  * Python side에서 TCP 통신으로 'lowerFinger', 즉 스크린을 터치하고 있는 손가락 index 값을 받는다.
  * 중지로 Trackpad를 조작할 경우 기본적인 cursor manipulation만을 수행한다.
  * 검지로 Trackpad를 조작할 경우 OCR(Optical character recognition)을 함께 수행한다. (어떤 alphabet을 그리는 지 순간적으로 감지)

## Main PC의 Cursor Manipulation 
1. Android와 TCP 통신으로 연결한다.
```c#
TcpListener listener = new TcpListener(IPAddress.Any, sPort);   // TCP 연결 Listener 생성
listener.Start();                                               // TCP listener 시작
TcpClient tc = listener.AcceptTcpClient();  // Client와 연결
NetworkStream stream = tc.GetStream();      // TCP Client로부터 NetworkStream을 받아옴
```
2. 반복해서 Data를 Read할 루프를 만든다.
```c#
while (true) {
  if ((nbytes = stream.Read(buff,0,16)) != 0) {
```
3. (루프 안) buff에 저장된 x, y, dX, dY, mode(DOWN,MOVE,UP)값을 받아온다. 
```c#
    byte[] x_bytes = { buff[0], buff[1], buff[2], buff[3] };
    byte[] y_bytes = { buff[4], buff[5], buff[6], buff[7] };
    byte[] dx_bytes = { buff[8], buff[9], buff[10], buff[11] };
    byte[] dy_bytes = { buff[12], buff[13], buff[14], buff[15] };
    byte[] mode_bytes = { buff[16], buff[17], buff[18], buff[19] };
    if (BitConverter.IsLittleEndian)
    {
      Array.Reverse(x_bytes);
      Array.Reverse(y_bytes);
      Array.Reverse(dx_bytes);
      Array.Reverse(dy_bytes);
      Array.Reverse(mode_bytes);
    }
    x = BitConverter.ToInt32(x_bytes, 0);
    y = BitConverter.ToInt32(y_bytes, 0);
    dx = BitConverter.ToInt32(dx_bytes, 0);
    dy = BitConverter.ToInt32(dy_bytes, 0);
    mode = BitConverter.ToInt32(mode_bytes, 0);
```
4. [Windows API](../study/windowsAPI/readme.md)를 사용해서 마우스 커서를 이동시킨다.
```c#
    WrapNative.Move(WrapNative.cursorPos().X + dx, WrapNative.cursorPos().Y + dy);
  }
}
```
## Gesture Recognition
1. MainWindow에 InkCanvas 컨트롤을 추가한다.
```c#
 <InkCanvas Name="theInkCanvas"></InkCanvas>  // Mainwindow.xaml
```
2. recognize() 함수를 만든다.
```c#
private void recognize(){  // TcpData Class의 메소드로 작성됨.
```
3. InkCanvas에 저장된 stroke들을 Ink 객체에 Load한다.
```c#
  using (MemoryStream ms = new MemoryStream())
  {
     target.invokeStrokeSave(ms);
     var ink = new Ink();
     ink.Load(ms.ToArray());
```
4. RecognizerContext 객체를 만들고 알파벳('a'~'z')만 인식하도록 설정해준다.
```c#
    using (RecognizerContext context = new RecognizerContext())
    {
       UnicodeRange ucRange = new UnicodeRange('a', 26);
       context.SetEnabledUnicodeRanges(new UnicodeRange[] { ucRange });
```
5. RecognizerContext.Recognize() 메소드를 이용해서 OCR을 수행한다.
```c#
       if (ink.Strokes.Count > 0)
       {
          context.Strokes = ink.Strokes;
          RecognitionStatus status;
          var result = context.Recognize(out status);
```
6. RecognitionResult.TopString 속성을 이용해서 인식된 text를 사용한다.
```c#
          if (status == RecognitionStatus.NoError)
            target.invokeTextBox1(result.TopString);
          else
            MessageBox.Show("Recognition failed");
       }
       else
         MessageBox.Show("No stroke detected");
    }
  }
```
7. Recognition이 끝나면 InkCanvas를 Clear해준다.
```c#
  target.invokeClear();
}
```