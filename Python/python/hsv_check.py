
# -*- coding: utf-8 -*-

import numpy as np
import cv2
import imutils
from PIL import Image

hueline = cv2.imread("hueline.png")

# Hue는 [0,179], Saturation은 [0,255], Value는 [0,255]로 표현이 됩니다.


redLower1 = (179, 100, 50)
redUpper1 = (180, 255, 255)

redLower2 = (179, 100, 50)
redUpper2 = (180, 255, 255)


blueLower = (100, 100, 50)
blueUpper = (110, 255, 255)

yellowLower = (20, 100, 50)
yellowUpper = (40, 255, 255)

selectedColorLower = redLower1
selectedColorUpper = redUpper1

img = np.asarray(hueline)
hsv_color_img = cv2.cvtColor(img, cv2.COLOR_BGR2HSV);

mask = cv2.inRange(hsv_color_img, selectedColorLower, selectedColorUpper)

mask2 = cv2.inRange(hsv_color_img, redLower2, redUpper2)
mask = mask+mask2

mask_3d = np.dstack((mask, mask, mask))  # depth image is 1 channel, color is 3 channels


maskedImg = np.where((mask_3d < 255), mask_3d, img)

cv2.imshow('RealSense', maskedImg)
cv2.waitKey(0)

