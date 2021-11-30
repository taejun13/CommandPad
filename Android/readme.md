## Android side
Android 코드에서 수행되는 기능은 다음과 같다.

* **Trackpad Simulation**
  * Trackpad의 영역을 만들고 손가락으로 터치되는 좌표를 TCP 통신으로 Main PC에 전송한다.

## Trackpad Simulation
1. 새로운 쓰레드를 열어서 Main PC와 TCP socket을 연결한다. (socket의 DataOutputStream도 받아온다)
```Java
new Thread() {      // onCreate() 내부
    public void run() {
        try {
            socket = new Socket("143.248.56.106", 5000);
            socket.setTcpNoDelay(true);     // Nagle's algorithm(Learn:Q3)를 끈다.
            dos = new DataOutputStream(socket.getOutputStream());
        }
        catch(IOException e){
            e.printStackTrace();
        }
    }
}.start();
```
2. LinearLayout 영역에 TouchListener를 추가해서 터치 좌표를 저장한다. (터치하는 순간마다 좌표를 전송하기 위해서)
```Java
layout.setOnTouchListener(new View.OnTouchListener() {
    @Override
    public boolean onTouch(View v, MotionEvent event) {
        double x = (double)event.getX();
        double y = (double)event.getY();
```
2. 연결된 socket의 DataOutputStream을 받아온다.
```Java
dos = new DataOutputStream(socket.getOutputStream());
```
3. x, y, dx, dy, mode값을 byte array로 만든다.
```Java
mode = 2;  // mode = 1 (ACTION_DOWN), mode = 2 (ACTION_MOVE), mode = 3(ACTION_UP)
x_int = (int)Math.round(x);  // x좌표 절댓값
y_int = (int)Math.round(y);  // y좌표 절댓값
dx = (int)Math.round(delta_x);  // x좌표 변화량
dy = (int)Math.round(delta_y);  // y좌표 변화량 
                           
byte[] bytes = FiveIntToByteArray(dx, dy);   //int 자료형 dx, dy를 byte array에 4바이트씩 넣음

public static final byte[] FiveIntToByteArray(int x, int y, int dx, int dy, int mode) {
        return new byte[]{
                (byte) (x >>> 24),
                (byte) (x >>> 16),
                (byte) (x >>> 8),
                (byte) x,
                (byte) (y >>> 24),
                (byte) (y >>> 16),
                (byte) (y >>> 8),
                (byte) y,
                (byte) (dx >>> 24),
                (byte) (dx >>> 16),
                (byte) (dx >>> 8),
                (byte) dx,
                (byte) (dy >>> 24),
                (byte) (dy >>> 16),
                (byte) (dy >>> 8),
                (byte) dy,
                (byte) (mode >>> 24),
                (byte) (mode >>> 16),
                (byte) (mode >>> 8),
                (byte) mode
        };
    }
}
```
4. DataOutputStream을 통해 byte array를 보낸다.
```Java
try {
    dos.write(bytes);
    dos.flush();
}
catch(IOException e){
    e.printStackTrace();
}
```