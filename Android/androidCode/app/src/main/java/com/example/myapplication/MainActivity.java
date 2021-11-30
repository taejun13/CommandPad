package com.example.myapplication;


import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.MotionEvent;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;

public class MainActivity extends AppCompatActivity {


    TextView text1;
    TextView text4;
    TextView text7;
    TextView text8;
    LinearLayout layout;


    Socket socket;
    DataOutputStream dos;

    double old_x = -9999;
    double old_y = -9999;
    int x_int;
    int y_int;
    int dx;
    int dy;
    int mode;
    byte[] bytes;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);



        setContentView(R.layout.activity_main);

        text1 = (TextView) findViewById(R.id.text1);
        text4 = (TextView) findViewById(R.id.text4);
        text7 = (TextView) findViewById(R.id.text7);
        text8 = (TextView) findViewById(R.id.text8);
        layout = (LinearLayout)findViewById(R.id.layout);


        new Thread() {
            public void run() {
                try {
                    socket = new Socket("143.248.56.106", 5000);
                    socket.setTcpNoDelay(true);
                    dos = new DataOutputStream(socket.getOutputStream());
                }
                catch(IOException e){
                    e.printStackTrace();
                }
            }
        }.start();

        layout.setOnTouchListener(new View.OnTouchListener() {
            @Override
            public boolean onTouch(View v, MotionEvent event) {
                double x = (double)event.getX();
                double y = (double)event.getY();
                if(x>=0 && y >= 0 && x<=1000 && y<=1000){
                    switch (event.getAction()){
                        case MotionEvent.ACTION_DOWN:   // mode 1
                            mode = 1;
                            // Trash values
                            x_int = 9999;
                            y_int = 9999;
                            dx = 9999;
                            dy = 9999;

                            bytes = FiveIntToByteArray(x_int,y_int,dx,dy,mode);
                            try {
                                dos.write(bytes);
                                dos.flush();
                            }
                            catch(IOException e){
                                e.printStackTrace();
                            }
                            old_x = x;
                            old_y = y;
                            break;
                        case MotionEvent.ACTION_MOVE:   // mode 2
                            double delta_x = x - old_x;
                            double delta_y = y - old_y;

                            mode = 2;
                            x_int = (int)Math.round(x);
                            y_int = (int)Math.round(y);
                            dx = (int)Math.round(delta_x);
                            dy = (int)Math.round(delta_y);

                            bytes = FiveIntToByteArray(x_int,y_int,dx,dy,mode);
                            try {
                                dos.write(bytes);
                                dos.flush();
                            }
                            catch(IOException e){
                                e.printStackTrace();
                            }
                            old_x = x;
                            old_y = y;
                            break;
                        case MotionEvent.ACTION_UP:     // mode 3
                            mode = 3;
                            // Trash values
                            x_int = 9999;
                            y_int = 9999;
                            dx = 9999;
                            dy = 9999;

                            bytes = FiveIntToByteArray(x_int,y_int,dx,dy,mode);
                            try {
                                dos.write(bytes);
                                dos.flush();
                            }
                            catch(IOException e){
                                e.printStackTrace();
                            }
                            old_x = x;
                            old_y = y;
                            break;
                    }
                }
                return true;
            }
        });

    }

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