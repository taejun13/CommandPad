## Python side
Python 코드에서 수행되는 기능은 다음과 같다.

* **Finger Identification** (Intel Realsense D415 Depth Camera 사용)
  * 스크린을 터치하는 있는 손가락의 index를 실시간으로 알아내는 것이 목적 
  * Color Tracking : 손가락의 color ring으로 각 손가락의 위치를 알아낸다.
  * Depth Clipping : 배경의 색이 Color Tracking을 방해하지 않도록 특정 거리 이상은 Clipping한다
  
## Finger Identification
* Intel Realsense SDK 2.0의 [python example](https://github.com/IntelRealSense/librealsense/blob/master/wrappers/python/examples/align-depth2color.py)사용 

1. Realsense D415 카메라의 pipeline을 받아오고 streaming을 시작한다.
```Python
pipeline = rs.pipeline()         # 카메라 pipeline 객체 받아오기
profile = pipeline.start(config) # streaming 시작
```
2. Depth scale을 받아와서 clipping_distance를 저장한다.
```Python
depth_sensor = profile.get_device().first_depth_sensor()
depth_scale = depth_sensor.get_depth_scale()
clipping_distance_in_meters = 0.3     # 30cm 이상은 Clipping
clipping_distance = clipping_distance_in_meters / depth_scale
```
3. clipping distance(30cm)보다 먼 곳은 grey로, 아닌 곳은 color 이미지로 bg_removed에 저장한다. **Depth Clipping 끝**
```Python
depth_image = np.asanyarray(aligned_depth_frame.get_data())  # image를 numpy array로 변환
color_image = np.asanyarray(color_frame.get_data())          # image를 numpy array로 변환
grey_color = 153
depth_image_3d = np.dstack((depth_image, depth_image, depth_image))  # depth image는 1채널이므로 3채널로 만듦.
bg_removed = np.where((depth_image_3d > clipping_distance) | (depth_image_3d <= 0), grey_color, color_image)
```
4. tracking할 색의 hsv 범위를 지정한다. **Color Tracking 시작**
```Python
blueLower = (90, 100, 50)
blueUpper = (110, 255, 255)
```
5. bg_removed를 HSV space로 변환하고 해당 색 범위의 mask(흑백 이미지)를 저장한다.
```Python
hsv_final = cv2.cvtColor(bg_removed, cv2.COLOR_BGR2HSV)
mask = cv2.inRange(hsv_final, blueLower, blueUpper)
```
6. 해당 색으로 이뤄진 영역을 '가장 작게 감싸는 원(minEnclosingCircle)'의 중심 좌표를 구한다. **Color Tracking 끝**
```Python
cnts_blue = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE) # mask의 윤곽들 저장
cnts_blue = imutils.grab_contours(cnts_blue)
c_blue = max(cnts_blue, key=cv2.contourArea)   # Contour Area가 가장 큰 Contour를 추출
((x, y), radius) = cv2.minEnclosingCircle(c_blue)  # 해당 영역을 '가장 작게 감싸는 원'의 중심 좌표와 반지름 
M = cv2.moments(c_blue)
center_blue = (int(M["m10"] / M["m00"]), int(M["m01"] / M["m00"]))
```