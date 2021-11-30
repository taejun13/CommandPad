## License: Apache 2.0. See LICENSE file in root directory.
## Copyright(c) 2015-2017 Intel Corporation. All Rights Reserved.

###############################################
##      Open CV and Numpy integration        ##
###############################################

import socket
import pyrealsense2 as rs
import numpy as np
import cv2
import imutils

# This program aims to determine the variable 'lowerfinger', whether 0(yellow), 1(blue)
lowerFinger = -1

def oneIntToByteArray(lowerfinger) :

    bytes = bytearray(4)
    bytes[0] = (lowerfinger >> 24) & 0xff
    bytes[1] = (lowerfinger >> 16) & 0xff
    bytes[2] = (lowerfinger >> 8) & 0xff
    bytes[3] = lowerfinger & 0xff

    return bytes


sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('127.0.0.1', 6000))

# Configure depth and color streams
pipeline = rs.pipeline()
config = rs.config()

config.enable_stream(rs.stream.depth, 480, 270, rs.format.z16, 30)  # minimum resolution for close detection
config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)


# Start streaming
profile = pipeline.start(config)

# Getting the depth sensor's depth scale (see rs-align example for explanation)
depth_sensor = profile.get_device().first_depth_sensor()
depth_scale = depth_sensor.get_depth_scale()
print("Depth Scale is: ", depth_scale)

# We will be removing the background of objects more than
#  clipping_distance_in_meters meters away
clipping_distance_in_meters = 0.3  # 1 meter
clipping_distance = clipping_distance_in_meters / depth_scale

# Create an align object
# rs.align allows us to perform alignment of depth frames to others frames
# The "align_to" is the stream type to which we plan to align depth frames.
align_to = rs.stream.color
align = rs.align(align_to)

# Streaming loop
try:
    while True:

        # Wait for a coherent pair of frames: depth and color
        frames = pipeline.wait_for_frames()

        # Align the depth frame to color frame
        aligned_frames = align.process(frames)

        # Get aligned frames
        aligned_depth_frame = aligned_frames.get_depth_frame()  # aligned_depth_frame is a 640x480 depth image
        color_frame = aligned_frames.get_color_frame()

        # Validate that both frames are valid
        if not aligned_depth_frame or not color_frame:
            continue

        # Convert images to numpy arrays
        depth_image = np.asanyarray(aligned_depth_frame.get_data())
        color_image = np.asanyarray(color_frame.get_data())

        depth_image = imutils.resize(depth_image, width=400)
        color_image = imutils.resize(color_image, width=400)

        # Remove background - Set pixels further than clipping_distance to grey
        grey_color = 153
        depth_image_3d = np.dstack(
            (depth_image, depth_image, depth_image))  # depth image is 1 channel, color is 3 channels
        bg_removed = np.where((depth_image_3d > clipping_distance) | (depth_image_3d <= 0), grey_color, color_image)

        # Render images
        depth_colormap = cv2.applyColorMap(cv2.convertScaleAbs(depth_image, alpha=0.03), cv2.COLORMAP_BONE)


        redLower1 = (175, 100, 50)
        redUpper1 = (180 ,255, 255)
        redLower2 = (0, 100, 50)
        redUpper2 = (2, 255, 255)

        blueLower = (100, 100, 50)
        blueUpper = (110, 255, 255)

        yellowLower = (20, 100, 50)
        yellowUpper = (40, 255, 255)

        greenLower = (70,80,20)
        greenUpper = (90,255,255)



        # resize the frame, blur it, and convert it to the HSV
        # color space
        blurred_img = cv2.GaussianBlur(bg_removed, (11,11),0)
        hsv_img = cv2.cvtColor(blurred_img, cv2.COLOR_BGR2HSV)

        # mask for each color
        # red
        mask_red = cv2.inRange(hsv_img, redLower1, redUpper1)
        mask_red2 = cv2.inRange(hsv_img, redLower2, redUpper2)
        mask_red = mask_red + mask_red2
        mask_red = cv2.erode(mask_red, None, iterations=2)
        mask_red = cv2.dilate(mask_red, None, iterations=2)

        # blue
        mask_blue = cv2.inRange(hsv_img, blueLower, blueUpper)
        mask_blue = cv2.erode(mask_blue, None, iterations=2)
        mask_blue = cv2.dilate(mask_blue, None, iterations=2)

        # yellow
        mask_yellow = cv2.inRange(hsv_img, yellowLower, yellowUpper)
        mask_yellow = cv2.erode(mask_yellow, None, iterations=2)
        mask_yellow = cv2.dilate(mask_yellow, None, iterations=2)

        # green
        mask_green = cv2.inRange(hsv_img, greenLower, greenUpper)
        mask_green = cv2.erode(mask_green, None, iterations=2)
        mask_green = cv2.dilate(mask_green, None, iterations=2)


        mask1 = mask_red
        mask2 = mask_blue

        # find contours in the mask and initialize the current
        # (x, y) center of the ball
        cnts_mask1 = cv2.findContours(mask1, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        cnts_mask1 = imutils.grab_contours(cnts_mask1)
        center_mask1 = None

        cnts_mask2 = cv2.findContours(mask2, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        cnts_mask2 = imutils.grab_contours(cnts_mask2)
        center_mask2 = None

        x_mask1, y_mask1, x_mask2, y_mask2 = (0, 0, 0, 0)
        # only proceed if at least one blue contour was found
        if len(cnts_mask1) > 0:
            # find the largest contour in the mask, then use
            # it to compute the minimum enclosing circle and
            # centroid
            c_mask1 = max(cnts_mask1, key=cv2.contourArea)
            ((x_mask1, y_mask1), radius_mask1) = cv2.minEnclosingCircle(c_mask1)
            M_mask1 = cv2.moments(c_mask1)
            center_mask1 = (int(M_mask1["m10"] / M_mask1["m00"]), int(M_mask1["m01"] / M_mask1["m00"]))

            # only proceed if the radius meets a minimum size
            if radius_mask1 > 1:
                # draw the circle and centroid on the frame,
                # then update the list of tracked points
                cv2.circle(bg_removed, (int(x_mask1), int(y_mask1)), int(radius_mask1),
                           (0, 255, 255), 2)
                cv2.circle(bg_removed, center_mask1, 5, (0, 0, 255), -1)

        # only proceed if at least one yellow contour was found
        if len(cnts_mask2) > 0:
            # find the largest contour in the mask, then use
            # it to compute the minimum enclosing circle and
            # centroid
            c_mask2 = max(cnts_mask2, key=cv2.contourArea)
            ((x_mask2, y_mask2), radius_mask2) = cv2.minEnclosingCircle(c_mask2)
            M_mask2 = cv2.moments(c_mask2)
            center_mask2 = (int(M_mask2["m10"] / M_mask2["m00"]), int(M_mask2["m01"] / M_mask2["m00"]))

            if radius_mask2 > 1:
                # draw the circle and centroid on the frame,
                # then update the list of tracked points
                cv2.circle(bg_removed, (int(x_mask2), int(y_mask2)), int(radius_mask2),
                           (0, 255, 255), 2)
                cv2.circle(bg_removed, center_mask2, 5, (0, 0, 255), -1)


        mask = mask1+mask2
        mask_3d = np.dstack((mask, mask, mask))
        images = np.hstack((color_image,mask_3d, bg_removed))



        # Show images
        cv2.namedWindow('RealSense', cv2.WINDOW_AUTOSIZE)
        cv2.imshow('RealSense', images)

        cv2.waitKey(1)

        if y_mask1 != 0 and y_mask2 != 0 :
            if y_mask1 > y_mask2 :
                print("Red is on lowest position")
                lowerFinger = 0
            else :
                print("Blue is on lowest position")
                lowerFinger = 1

        bytes = oneIntToByteArray(lowerFinger)
        sock.send(bytes)

finally:

    # Stop streaming
    pipeline.stop()

