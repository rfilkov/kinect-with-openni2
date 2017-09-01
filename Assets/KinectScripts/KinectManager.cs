using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 

public class KinectManager : MonoBehaviour
{
	// Public Bool to determine how many players there are. Default of one user.
	public bool TwoUsers = false;
	
	// Public Bool to determine if the sensor is used in near mode.
	public bool NearMode = false;
	
	// Public Bool to determine whether to receive and compute the user map
	public bool ComputeUserMap = false;
	
	// Public Bool to determine whether to receive and compute the color map
	public bool ComputeColorMap = false;
	
	// Public Bool to determine whether to receive and compute the infrared map
	public bool ComputeInfraredMap = false;
	
	// Public Bool to determine whether to display user map on the GUI
	public bool DisplayUserMap = false;
	
	// Public Bool to determine whether to display color map on the GUI
	public bool DisplayColorMap = false;
	
	// Public Float to specify the image width used by deptsh and color maps as % of the camera width. the height is calculated depending on the width.
	// if percents are zero, they are calculated based on actual Kinect image?s width and height
	public float DisplayMapsWidthPercent = 20f;

	// How high off the ground is the sensor (in meters)
	public float SensorHeight = 1.0f;
	
	// Bools to keep track of who is currently calibrated.
	private bool Player1Calibrated = false;
	private bool Player2Calibrated = false;
	
	private bool AllPlayersCalibrated = false;
	
	// Values to track which ID (assigned by the Kinect) is player 1 and player 2.
	private uint Player1ID;
	private uint Player2ID;
	
	// Lists of GameObjects that will be controlled by which player.
	public List<GameObject> Player1Avatars;
	public List<GameObject> Player2Avatars;
	
	// Lists of AvatarControllers that will let the models get updated.
	private List<AvatarController> Player1Controllers;
	private List<AvatarController> Player2Controllers;
	
	// Calibration poses for each player, if needed
	public KinectGestures.Gestures Player1CalibrationPose;
	public KinectGestures.Gestures Player2CalibrationPose;
	
	// List of Gestures to be detected for each player
	public List<KinectGestures.Gestures> Player1Gestures;
	public List<KinectGestures.Gestures> Player2Gestures;

	// Minimum time between gesture detections
	public float MinTimeBetweenGestures = 0.7f;
	
	// List of Gesture Listeners. They must implement KinectGestures.GestureListenerInterface
	public List<MonoBehaviour> GestureListeners;
	
	// GUI Text to show messages.
	public GameObject CalibrationText;
	
	// GUI Texture to display the hand cursor for Player1
	public GameObject HandCursor1;
	
	// GUI Texture to display the hand cursor for Player2
	public GameObject HandCursor2;
	
	// Bool to specify whether Left/Right-hand-cursor and the Click-gesture control the mouse cursor and click
	public bool ControlMouseCursor = false;
	
	private KinectWrapper.SkeletonJointTransformation jointTransform;
	private KinectWrapper.SkeletonJointPosition jointPosition;
	private KinectWrapper.SkeletonJointOrientation jointOrientation;
	
	// User/Depth map variables
	private Texture2D usersLblTex;
	private Color[] usersMapColors;
	private Rect usersMapRect;
	private int usersMapSize;
	private short[] usersLabelMap;
	private short[] usersDepthMap;
	private float[] usersHistogramMap;

	private int usersInfraredMapSize;
	private short[] usersInfraredMap;

	// Color map variables
	private Texture2D usersClrTex;
	private Color32[] usersClrColors;
	private Rect usersClrRect;
	private int usersClrSize;
	private byte[] usersColorMap;
	
	// List of all users
	private List<uint> allUsers;
	
	// Bool to keep track of whether OpenNI has been initialized
	private bool KinectInitialized = false; 
	
	// The single instance of KinectManager
	private static KinectManager instance;

	private short[] oniUsers = new short[KinectWrapper.Constants.SkeletonCount];
	private short[] oniStates = new short[KinectWrapper.Constants.SkeletonCount];
	private Int32 oniUsersCount = 0;
	
	// Calibration gesture data for each player
	private KinectGestures.GestureData player1CalibrationData;
	private KinectGestures.GestureData player2CalibrationData;
	
	// PlayerIDs after calibration, but before calibration pose succeeds
	private List<uint> alCalibratedPlayerID = new List<uint>();
	
	// gestures data and parameters
	private List<KinectGestures.GestureData> player1Gestures = new List<KinectGestures.GestureData>();
	private List<KinectGestures.GestureData> player2Gestures = new List<KinectGestures.GestureData>();
	private float gestureTrackingAtTime1 = 0f, gestureTrackingAtTime2 = 0f;
	
	// List of Gesture Listeners. They must implement KinectGestures.GestureListenerInterface
	public List<KinectGestures.GestureListenerInterface> gestureListeners;
	
	
	// returns the single KinectManager instance
    public static KinectManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// checks if Kinect is initialized and ready to use. If not, there was an error during Kinect-sensor initialization
	public static bool IsKinectInitialized()
	{
		return instance != null ? instance.KinectInitialized : false;
	}
	
	// checks if Kinect is initialized and ready to use. If not, there was an error during Kinect-sensor initialization
	public bool IsInitialized()
	{
		return KinectInitialized;
	}
	
	// this function is used internally by AvatarController
	public static bool IsCalibrationNeeded()
	{
		return true;
	}
	
	// returns the raw depth/user data, if ComputeUserMap is true
	public short[] GetUsersDepthMap()
	{
		return usersDepthMap;
	}
	
	// returns the raw infrared data, if ComputeInfraredMap is true
	public short[] GetUsersInfraredMap()
	{
		return usersInfraredMap;
	}
	
	// returns the depth image/users histogram texture, if ComputeUserMap is true
    public Texture2D GetUsersLblTex()
    { 
		return usersLblTex;
	}
	
	// returns the color image texture, if ComputeColorMap is true
    public Texture2D GetUsersClrTex()
    { 
		return usersClrTex;
	}
	
	// returns true if at least one user is currently detected by the sensor
	public bool IsUserDetected()
	{
		return KinectInitialized && (allUsers.Count > 0);
	}
	
	// returns the UserID of Player1, or 0 if no Player1 is detected
	public uint GetPlayer1ID()
	{
		return Player1ID;
	}
	
	// returns the UserID of Player2, or 0 if no Player2 is detected
	public uint GetPlayer2ID()
	{
		return Player2ID;
	}
	
	// returns true if the User is calibrated and ready to use
	public bool IsPlayerCalibrated(uint UserId)
	{
		if(UserId == Player1ID)
			return Player1Calibrated;
		else if(UserId == Player2ID)
			return Player2Calibrated;
		
		return false;
	}
	
	// returns the User position, relative to the Kinect-sensor, in meters
	public Vector3 GetUserPosition(uint UserId)
	{
		if(KinectWrapper.GetJointPosition(UserId, (int)KinectWrapper.SkeletonJoint.HIPS, ref jointPosition))
		{
			return new Vector3(jointPosition.x * 0.001f, jointPosition.y * 0.001f + SensorHeight, jointPosition.z * 0.001f);
		}
		
		return Vector3.zero;
	}
	
	// returns the User rotation, relative to the Kinect-sensor
	public Quaternion GetUserOrientation(uint UserId, bool flip)
	{
		Quaternion rotUser = Quaternion.identity;
		
		if(KinectWrapper.GetSkeletonJointOrientation(UserId, (int)KinectWrapper.SkeletonJoint.HIPS, flip, ref rotUser))
		{
			//Quaternion quat = ConvertMatrixToQuat(jointOrientation, (int)KinectWrapper.SkeletonJoint.HIPS, flip);
			//return quat;
			return rotUser;
		}
		
		return Quaternion.identity;
	}
	
	// returns true if the given joint's position is being tracked
	public bool IsJointPositionTracked(uint UserId, int joint)
	{
		float fConfidence = KinectWrapper.GetJointPositionConfidence(UserId, joint);
		return fConfidence > 0.5;
	}
	
	// returns true if the given joint's orientation is being tracked
	public bool IsJointOrientationTracked(uint UserId, int joint)
	{
		float fConfidence = KinectWrapper.GetJointOrientationConfidence(UserId, joint);
		return fConfidence > 0.5;
	}
	
	// returns the joint position of the specified user, relative to the Kinect-sensor, in meters
	public Vector3 GetJointPosition(uint UserId, int joint)
	{
		if(KinectWrapper.GetJointPosition(UserId, joint, ref jointPosition))
		{
			return new Vector3(jointPosition.x * 0.001f, jointPosition.y * 0.001f + SensorHeight, jointPosition.z * 0.001f);
		}

		return Vector3.zero;
	}
	
	// returns the joint rotation of the specified user, relative to the Kinect-sensor
	public Quaternion GetJointOrientation(uint UserId, int joint, bool flip)
	{
		Quaternion rotJoint = Quaternion.identity;
		
		if(KinectWrapper.GetSkeletonJointOrientation(UserId, joint, flip, ref rotJoint))
		{
			//Quaternion quat = ConvertMatrixToQuat(jointOrientation, joint, flip);
			//return quat;
			return rotJoint;
		}

		return Quaternion.identity;
	}
	
	// adds a gesture to the list of detected gestures for the specified user
	public void DetectGesture(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index >= 0)
			DeleteGesture(UserId, gesture);
		
		KinectGestures.GestureData gestureData = new KinectGestures.GestureData();
		
		gestureData.userId = UserId;
		gestureData.gesture = gesture;
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		
		gestureData.checkForGestures = new List<KinectGestures.Gestures>();
		switch(gesture)
		{
			case KinectGestures.Gestures.ZoomIn:
				gestureData.checkForGestures.Add(KinectGestures.Gestures.ZoomOut);
				gestureData.checkForGestures.Add(KinectGestures.Gestures.Wheel);			
				break;

			case KinectGestures.Gestures.ZoomOut:
				gestureData.checkForGestures.Add(KinectGestures.Gestures.ZoomIn);
				gestureData.checkForGestures.Add(KinectGestures.Gestures.Wheel);			
				break;

			case KinectGestures.Gestures.Wheel:
				gestureData.checkForGestures.Add(KinectGestures.Gestures.ZoomIn);
				gestureData.checkForGestures.Add(KinectGestures.Gestures.ZoomOut);			
				break;
			
//			case KinectGestures.Gestures.Jump:
//				gestureData.checkForGestures.Add(KinectGestures.Gestures.Squat);
//				break;
//				
//			case KinectGestures.Gestures.Squat:
//				gestureData.checkForGestures.Add(KinectGestures.Gestures.Jump);
//				break;
//				
//			case KinectGestures.Gestures.Push:
//				gestureData.checkForGestures.Add(KinectGestures.Gestures.Pull);
//				break;
//				
//			case KinectGestures.Gestures.Pull:
//				gestureData.checkForGestures.Add(KinectGestures.Gestures.Push);
//				break;
		}
		
		if(UserId == Player1ID)
			player1Gestures.Add(gestureData);
		else if(UserId == Player2ID)
			player2Gestures.Add(gestureData);
	}
	
	// resets the gesture-data state for the given gesture of the specified user
	public bool ResetGesture(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		KinectGestures.GestureData gestureData = (UserId == Player1ID) ? player1Gestures[index] : player2Gestures[index];
		
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		gestureData.startTrackingAtTime = Time.realtimeSinceStartup + KinectWrapper.Constants.MinTimeBetweenSameGestures;

		if(UserId == Player1ID)
			player1Gestures[index] = gestureData;
		else if(UserId == Player2ID)
			player2Gestures[index] = gestureData;
		
		return true;
	}
	
	// resets the gesture-data states for all detected gestures of the specified user
	public void ResetPlayerGestures(uint UserId)
	{
		if(UserId == Player1ID)
		{
			int listSize = player1Gestures.Count;
			
			for(int i = 0; i < listSize; i++)
			{
				ResetGesture(UserId, player1Gestures[i].gesture);
			}
		}
		else if(UserId == Player2ID)
		{
			int listSize = player2Gestures.Count;
			
			for(int i = 0; i < listSize; i++)
			{
				ResetGesture(UserId, player2Gestures[i].gesture);
			}
		}
	}
	
	// deletes the given gesture from the list of detected gestures for the specified user
	public bool DeleteGesture(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		if(UserId == Player1ID)
			player1Gestures.RemoveAt(index);
		else if(UserId == Player2ID)
			player2Gestures.RemoveAt(index);
		
		return true;
	}
	
	// clears detected gestures list for the specified user
	public void ClearGestures(uint UserId)
	{
		if(UserId == Player1ID)
		{
			player1Gestures.Clear();
		}
		else if(UserId == Player2ID)
		{
			player2Gestures.Clear();
		}
	}
	
	// returns the count of detected gestures in the list of detected gestures for the specified user
	public int GetGesturesCount(uint UserId)
	{
		if(UserId == Player1ID)
			return player1Gestures.Count;
		else if(UserId == Player2ID)
			return player2Gestures.Count;
		
		return 0;
	}
	
	// returns the list of detected gestures for the specified user
	public List<KinectGestures.Gestures> GetGesturesList(uint UserId)
	{
		List<KinectGestures.Gestures> list = new List<KinectGestures.Gestures>();

		if(UserId == Player1ID)
		{
			foreach(KinectGestures.GestureData data in player1Gestures)
				list.Add(data.gesture);
		}
		else if(UserId == Player2ID)
		{
			foreach(KinectGestures.GestureData data in player1Gestures)
				list.Add(data.gesture);
		}
		
		return list;
	}
	
	// returns true, if the given gesture is in the list of tracked gestures for the specified user
	public bool IsTrackingGesture(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		return index >= 0;
	}
	
	// returns true, if the given gesture for the specified user is complete
	public bool IsGestureComplete(uint UserId, KinectGestures.Gestures gesture, bool bResetOnComplete)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectGestures.GestureData gestureData = player1Gestures[index];
				
				if(bResetOnComplete && gestureData.complete)
				{
					ResetPlayerGestures(UserId);
					return true;
				}
				
				return gestureData.complete;
			}
			else if(UserId == Player2ID)
			{
				KinectGestures.GestureData gestureData = player2Gestures[index];

				if(bResetOnComplete && gestureData.complete)
				{
					ResetPlayerGestures(UserId);
					return true;
				}
				
				return gestureData.complete;
			}
		}
		
		return false;
	}
	
	// returns true, if the given gesture for the specified user is cancelled
	public bool IsGestureCancelled(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectGestures.GestureData gestureData = player1Gestures[index];
				return gestureData.cancelled;
			}
			else if(UserId == Player2ID)
			{
				KinectGestures.GestureData gestureData = player2Gestures[index];
				return gestureData.cancelled;
			}
		}
		
		return false;
	}
	
	// returns the progress in range [0, 1] of the given gesture for the specified user
	public float GetGestureProgress(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectGestures.GestureData gestureData = player1Gestures[index];
				return gestureData.progress;
			}
			else if(UserId == Player2ID)
			{
				KinectGestures.GestureData gestureData = player2Gestures[index];
				return gestureData.progress;
			}
		}
		
		return 0f;
	}
	
	// returns the current "screen position" of the given gesture for the specified user
	public Vector3 GetGestureScreenPos(uint UserId, KinectGestures.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectGestures.GestureData gestureData = player1Gestures[index];
				return gestureData.screenPos;
			}
			else if(UserId == Player2ID)
			{
				KinectGestures.GestureData gestureData = player2Gestures[index];
				return gestureData.screenPos;
			}
		}
		
		return Vector3.zero;
	}
	
	// recreates and reinitializes the lists of avatar controllers, after the list of avatars for player 1/2 was changed
	public void ResetAvatarControllers()
	{
		if(Player1Avatars.Count == 0 && Player2Avatars.Count == 0)
		{
			AvatarController[] avatars = FindObjectsOfType(typeof(AvatarController)) as AvatarController[];
			
			foreach(AvatarController avatar in avatars)
			{
				Player1Avatars.Add(avatar.gameObject);
			}
		}
		
		if(Player1Controllers != null)
		{
			Player1Controllers.Clear();
	
			foreach(GameObject avatar in Player1Avatars)
			{
				if(avatar != null && avatar.activeInHierarchy)
				{
					AvatarController controller = avatar.GetComponent<AvatarController>();
					controller.RotateToInitialPosition();
					controller.Start();
					
					Player1Controllers.Add(controller);
				}
			}
		}
		
		if(Player2Controllers != null)
		{
			Player2Controllers.Clear();
			
			foreach(GameObject avatar in Player2Avatars)
			{
				if(avatar != null && avatar.activeInHierarchy)
				{
					AvatarController controller = avatar.GetComponent<AvatarController>();
					controller.RotateToInitialPosition();
					controller.Start();
					
					Player2Controllers.Add(controller);
				}
			}
		}
	}
	
	// removes the currently detected kinect users, allowing a new detection/calibration process to start
	public void ClearKinectUsers()
	{
		if(!KinectInitialized)
			return;

		// remove current users
		for(int i = allUsers.Count - 1; i >= 0; i--)
		{
			uint userId = allUsers[i];
			OnUserLost(userId);
		}
		
		//ResetFilters();
	}

	
	//----------------------------------- end of public functions --------------------------------------//

	void Awake() 
	{
		//CalibrationText = GameObject.Find("CalibrationText");
		
		try
		{
			if(KinectWrapper.CheckOpenNIPresence())
			{
				// reload the same level
				Application.LoadLevel(Application.loadedLevel);
			}

//			if(CalibrationText != null)
//			{
//				CalibrationText.GetComponent<GUIText>().font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
//			}
		} 
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = ex.Message;
			}
		}
		
	}

	void Start()
	{
		try 
		{
			// Initialize the OpenNI/NiTE wrapper
			int rc = KinectWrapper.Init(ComputeUserMap, ComputeColorMap, ComputeInfraredMap);
	        if (rc != 0)
	        {
	            throw new Exception(String.Format("Error initing OpenNI: {0}", Marshal.PtrToStringAnsi(KinectWrapper.GetLastErrorString())));
	        }
			
			// get the main camera rectangle
			Rect cameraRect = Camera.main.pixelRect;

			if(DisplayMapsWidthPercent == 0f)
			{
				DisplayMapsWidthPercent = (KinectWrapper.GetDepthWidth() / 2f) * 100f / cameraRect.width;
			}

			int depthMapWidth = KinectWrapper.GetDepthWidth() > 0 ? KinectWrapper.GetDepthWidth() : 640;
			int depthMapHeight = KinectWrapper.GetDepthHeight() > 0 ? KinectWrapper.GetDepthHeight() : 480;

			float displayMapsWidthPercent = DisplayMapsWidthPercent / 100f;
			float displayMapsHeightPercent = displayMapsWidthPercent * depthMapHeight / depthMapWidth;

			float displayWidth = cameraRect.width * displayMapsWidthPercent;
			float displayHeight = cameraRect.width * displayMapsHeightPercent;

			if(ComputeUserMap)
			{
		        // Initialize depth & label map related stuff
				usersMapSize = depthMapWidth * depthMapHeight;
				usersLblTex = new Texture2D(depthMapWidth, depthMapHeight);
		        usersMapColors = new Color[usersMapSize];
		        //usersMapRect = new Rect(Screen.width, Screen.height - usersLblTex.height / 2, -usersLblTex.width / 2, usersLblTex.height / 2);
				usersMapRect = new Rect(cameraRect.width, cameraRect.height - displayHeight, -displayWidth, displayHeight);
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
//				usersMapRect.x = cameraRect.width - displayWidth;
//				usersMapRect.width = displayWidth;
#endif

				usersLabelMap = new short[usersMapSize];
		        usersDepthMap = new short[usersMapSize];
		        usersHistogramMap = new float[8192];
			}

			if(ComputeInfraredMap)
			{
				usersInfraredMapSize = KinectWrapper.GetInfraredWidth() * KinectWrapper.GetInfraredHeight();
				usersInfraredMap = new short[usersInfraredMapSize];
			}
	
			if(ComputeColorMap)
			{
		        // Initialize color map related stuff
		        usersClrSize = KinectWrapper.GetColorWidth() * KinectWrapper.GetColorHeight();
		        usersClrTex = new Texture2D(KinectWrapper.GetColorWidth(), KinectWrapper.GetColorHeight());
		        usersClrColors = new Color32[usersClrSize];
		        //usersClrRect = new Rect(Screen.width, Screen.height - usersClrTex.height / 2, -usersClrTex.width / 2, usersClrTex.height / 2);
				usersClrRect = new Rect(cameraRect.width, cameraRect.height - displayHeight, -displayWidth, displayHeight);
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
//				usersClrRect.x = cameraRect.width - displayWidth;
//				usersClrRect.width = displayWidth;
#endif

				if(ComputeUserMap)
				{
					usersMapRect.x -= displayWidth; //usersClrTex.width / 2;
				}

				usersColorMap = new byte[usersClrSize * 3];
			}
	
	        // Initialize user list to contain ALL users.
	        allUsers = new List<uint>();
	        
//	        // Initialize user callbacks.
//	        NewUser = new KinectWrapper.UserDelegate(OnNewUser);
//	        CalibrationStarted = new KinectWrapper.UserDelegate(OnCalibrationStarted);
//	        CalibrationFailed = new KinectWrapper.UserDelegate(OnCalibrationFailed);
//	        CalibrationSuccess = new KinectWrapper.UserDelegate(OnCalibrationSuccess);
//	        UserLost = new KinectWrapper.UserDelegate(OnUserLost);
			
			// try to automatically find the available avatar controllers in the scene
			if(Player1Avatars.Count == 0 && Player2Avatars.Count == 0)
			{
				AvatarController[] avatars = FindObjectsOfType(typeof(AvatarController)) as AvatarController[];
				
				foreach(AvatarController avatar in avatars)
				{
					Player1Avatars.Add(avatar.gameObject);
				}
			}
			
			// Pull the AvatarController from each of the players Avatars.
			Player1Controllers = new List<AvatarController>();
			Player2Controllers = new List<AvatarController>();
			
			// Add each of the avatars' controllers into a list for each player.
			foreach(GameObject avatar in Player1Avatars)
			{
				Player1Controllers.Add(avatar.GetComponent<AvatarController>());
			}
			
			foreach(GameObject avatar in Player2Avatars)
			{
				Player2Controllers.Add(avatar.GetComponent<AvatarController>());
			}
			
			// try to automatically find the available gesture listeners in the scene
			if(GestureListeners.Count == 0)
			{
				MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
				
				foreach(MonoBehaviour monoScript in monoScripts)
				{
					if(typeof(KinectGestures.GestureListenerInterface).IsAssignableFrom(monoScript.GetType()))
					{
						GestureListeners.Add(monoScript);
					}
				}
			}
			// create the list of gesture listeners
			gestureListeners = new List<KinectGestures.GestureListenerInterface>();
			
			foreach(MonoBehaviour script in GestureListeners)
			{
				if(script && (script is KinectGestures.GestureListenerInterface))
				{
					KinectGestures.GestureListenerInterface listener = (KinectGestures.GestureListenerInterface)script;
					gestureListeners.Add(listener);
				}
			}
			
			// GUI Text.
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = "WAITING FOR USERS";
			}
			
	        // Start looking for users.
	        //KinectWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
			KinectWrapper.StartLookingForUsers(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			Debug.Log("Waiting for users to calibrate");
			
			// Set the default smoothing for the Kinect.
			KinectWrapper.SetSkeletonSmoothing(0.7f);
			
			instance = this;
			KinectInitialized = true;
			
			DontDestroyOnLoad(gameObject);
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(CalibrationText != null)
				CalibrationText.GetComponent<GUIText>().text = "Please check the OpenNI and NITE installations.";
		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(CalibrationText != null)
				CalibrationText.GetComponent<GUIText>().text = ex.Message;
		}
	}
	
	void Update()
	{
		if(KinectInitialized)
		{
	        // Update to the next frame.
			oniUsersCount = oniUsers.Length;
			KinectWrapper.Update(oniUsers, oniStates, ref oniUsersCount);
			
			// Process the new, lost and calibrated user(s)
			if(oniUsersCount > 0)
			{
				for(int i = 0; i < oniUsersCount; i++)
				{
					uint userId = (uint)oniUsers[i];
					short userState = oniStates[i];
					
					switch(userState)
					{
						case 1: // new user
							OnNewUser(userId);
							break;

						case 2: // calibration started
							OnCalibrationStarted(userId);
							break;

						case 3: // calibration succeeded
							OnCalibrationSuccessBeforePose(userId);
							break;

						case 4: // calibration failed
							OnCalibrationFailed(userId);
							break;

						case 5: // user lost
							OnUserLost(userId);
							break;

					}
				}
			}
	
			// Draw the color map
			if(ComputeColorMap)
			{
	        	UpdateColorMap();
			}
			
			// Draw the user map
			if(ComputeUserMap)
			{
				UpdateUserMap();
			}
			
			// Draw the infrared map
			if(ComputeInfraredMap)
			{
				UpdateInfraredMap();
			}
			
			if(alCalibratedPlayerID.Count > 0)
			{
				// check for the calibration pose
				for(int i = alCalibratedPlayerID.Count - 1; i >= 0; i--)
				{
					uint userId = alCalibratedPlayerID[i];
					
					if(OnCalibrationSuccessAfterPose(userId))
					{
						alCalibratedPlayerID.RemoveAt(i);
					}
				}
			}

			
			if(Player1Calibrated)
			{
				// Update player 1's models
				foreach (AvatarController controller in Player1Controllers)
				{
					//if(controller.Active)
					{
						controller.UpdateAvatar(Player1ID, NearMode);
					}
				}
				
				// Check for player 1's gestures
				CheckForGestures(Player1ID, ref player1Gestures, ref gestureTrackingAtTime1);

				// Check for complete gestures
				foreach(KinectGestures.GestureData gestureData in player1Gestures)
				{
					if(gestureData.complete)
					{
						if(gestureData.gesture == KinectGestures.Gestures.Click)
						{
							if(ControlMouseCursor)
							{
								MouseControl.MouseClick();
							}
						}
						
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							if(listener.GestureCompleted(Player1ID, 0, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetPlayerGestures(Player1ID);
							}
						}
					}
					else if(gestureData.cancelled)
					{
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							if(listener.GestureCancelled(Player1ID, 0, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint))
							{
								ResetGesture(Player1ID, gestureData.gesture);
							}
						}
					}
					else if(gestureData.progress >= 0.1f)
					{
						if((gestureData.gesture == KinectGestures.Gestures.RightHandCursor || 
							gestureData.gesture == KinectGestures.Gestures.LeftHandCursor) && 
							gestureData.progress >= 0.5f)
						{
							if(HandCursor1 != null)
							{
								HandCursor1.transform.position = Vector3.Lerp(HandCursor1.transform.position, gestureData.screenPos, 3 * Time.deltaTime);
							}
							
							if(ControlMouseCursor)
							{
								MouseControl.MouseMove(gestureData.screenPos);
							}
						}
			
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							listener.GestureInProgress(Player1ID, 0, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
			
			// Update player 2's models
			if(Player2Calibrated)
			{
				foreach (AvatarController controller in Player2Controllers)
				{
					//if(controller.Active)
					{
						controller.UpdateAvatar(Player2ID, NearMode);
					}
				}
				
				// Check for player 2's gestures
				CheckForGestures(Player2ID, ref player2Gestures, ref gestureTrackingAtTime2);

				// Check for complete gestures
				foreach(KinectGestures.GestureData gestureData in player2Gestures)
				{
					if(gestureData.complete)
					{
						if(gestureData.gesture == KinectGestures.Gestures.Click)
						{
							if(ControlMouseCursor)
							{
								MouseControl.MouseClick();
							}
						}
						
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							if(listener.GestureCompleted(Player2ID, 1, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetPlayerGestures(Player2ID);
							}
						}
					}
					else if(gestureData.cancelled)
					{
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							if(listener.GestureCancelled(Player2ID, 1, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint))
							{
								ResetGesture(Player2ID, gestureData.gesture);
							}
						}
					}
					else if(gestureData.progress >= 0.1f)
					{
						if((gestureData.gesture == KinectGestures.Gestures.RightHandCursor || 
							gestureData.gesture == KinectGestures.Gestures.LeftHandCursor) && 
							gestureData.progress >= 0.5f)
						{
							if(HandCursor2 != null)
							{
								HandCursor2.transform.position = Vector3.Lerp(HandCursor2.transform.position, gestureData.screenPos, 3 * Time.deltaTime);
							}

							if(ControlMouseCursor)
							{
								MouseControl.MouseMove(gestureData.screenPos);
							}
						}
						
						foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
						{
							listener.GestureInProgress(Player2ID, 1, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
			
		}
		
		// Kill the program with ESC.
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
	
	// Make sure to kill the Kinect on quitting.
	void OnApplicationQuit()
	{
		if(KinectInitialized)
		{
			// Shutdown OpenNI
			KinectWrapper.Shutdown();
			instance = null;
		}
	}
	
	// Draw the Histogram Map on the GUI.
    void OnGUI()
    {
		if(KinectInitialized)
		{
	        if(ComputeUserMap && (/**(allUsers.Count == 0) ||*/ DisplayUserMap))
	        {
	            GUI.DrawTexture(usersMapRect, usersLblTex);
	        }
			
	        if(ComputeColorMap && (/**(allUsers.Count == 0) ||*/ DisplayColorMap))
	        {
	            GUI.DrawTexture(usersClrRect, usersClrTex);
	        }
		}
    }
	
	// Update / draw the User Map
    void UpdateUserMap()
    {
		IntPtr pLabelMap = KinectWrapper.GetUsersLabelMap();
		IntPtr pDepthMap = KinectWrapper.GetUsersDepthMap();
		
		if(pLabelMap == IntPtr.Zero || pDepthMap == IntPtr.Zero)
			return;
		
        // copy over the maps
        Marshal.Copy(pLabelMap, usersLabelMap, 0, usersMapSize);
        Marshal.Copy(pDepthMap, usersDepthMap, 0, usersMapSize);

        // Flip the texture as we convert label map to color array
        int flipIndex, i;
        int numOfPoints = 0;
		Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // Calculate cumulative histogram for depth
        for (i = 0; i < usersMapSize; i++)
        {
            // Only calculate for depth that contains users
            if (usersLabelMap[i] != 0)
            {
                usersHistogramMap[usersDepthMap[i]]++;
                numOfPoints++;
            }
        }
		
        if (numOfPoints > 0)
        {
            for (i = 1; i < usersHistogramMap.Length; i++)
	        {   
		        usersHistogramMap[i] += usersHistogramMap[i-1];
	        }
            for (i = 0; i < usersHistogramMap.Length; i++)
	        {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
	        }
        }

        // Create the actual users texture based on label map and depth histogram
        for (i = 0; i < usersMapSize; i++)
        {
            flipIndex = usersMapSize - i - 1;
			
            if (usersLabelMap[i] == 0)
            {
                usersMapColors[flipIndex] = Color.clear;
            }
            else
            {
                // Create a blending color based on the depth histogram
				float histVal = usersHistogramMap[usersDepthMap[i]];
                Color c = new Color(histVal, histVal, histVal, 0.9f);
				
                switch (usersLabelMap[i] % 4)
                {
                    case 0:
                        usersMapColors[flipIndex] = Color.red * c;
                        break;
                    case 1:
                        usersMapColors[flipIndex] = Color.green * c;
                        break;
                    case 2:
                        usersMapColors[flipIndex] = Color.blue * c;
                        break;
                    case 3:
                        usersMapColors[flipIndex] = Color.magenta * c;
                        break;
                }
            }
        }
		
		// Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();
    }

	// Update infrared map
	void UpdateInfraredMap()
	{
		IntPtr pInfraredMap = KinectWrapper.GetUsersInfraredMap();
		
		if(pInfraredMap == IntPtr.Zero)
			return;
		
		// copy over the map
		Marshal.Copy(pInfraredMap, usersInfraredMap, 0, usersInfraredMapSize);
	}
	
	// Update / draw the User Map
    void UpdateColorMap()
    {
		IntPtr pColorMap = KinectWrapper.GetUsersColorMap();
		if(pColorMap == IntPtr.Zero)
			return;
		
        // copy over the map
        Marshal.Copy(pColorMap, usersColorMap, 0, usersClrSize * 3);

        // Flip the texture as we convert color map to color array
        int index = 0, flipIndex;

        // Create the actual users texture based on label map and depth histogram
        for (int i = 0; i < usersClrSize; i++)
        {
            flipIndex = usersClrSize - i - 1;
			
			usersClrColors[flipIndex].r = usersColorMap[index];
			usersClrColors[flipIndex].g = usersColorMap[index + 1];
			usersClrColors[flipIndex].b = usersColorMap[index + 2];
			usersClrColors[flipIndex].a = 230;
			
			index += 3;
        }
		
		// Draw it!
        usersClrTex.SetPixels32(usersClrColors);
        usersClrTex.Apply();
    }

//	// Add model to player list.
//	void AddAvatar(GameObject avatar, List<GameObject> whichPlayerList)
//	{
//		whichPlayerList.Add(avatar);
//	}
//	
//	// Remove model from player list.
//	void RemoveAvatar(GameObject avatar, List<GameObject> whichPlayerList)
//	{
//		whichPlayerList.Remove(avatar);
//	}
	
//	// Functions that let you recalibrate either player 1 or player 2.
//	void RecalibratePlayer1()
//	{
//		OnUserLost(Player1ID);
//	}
//	
//	void RecalibratePlayer2()
//	{
//		OnUserLost(Player2ID);
//	}
	
	// When a new user enters, add it to the list.
	void OnNewUser(uint UserId)
    {
        Debug.Log(String.Format("[{0}] New user", UserId));
    }   
	
	// Print out when the user begins calibration.
    void OnCalibrationStarted(uint UserId)
    {
		Debug.Log(String.Format("[{0}] Calibration started", UserId));
		
		if(CalibrationText != null)
		{
			//CalibrationText.GetComponent<GUIText>().text = "CALIBRATING...";
		}
    }
	
	// Alert us when the calibration fails.
    void OnCalibrationFailed(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration failed", UserId));
		
		if(CalibrationText != null)
		{
			CalibrationText.GetComponent<GUIText>().text = "WAITING FOR USERS";
		}
    }
	
	
	// If a user successfully calibrates, assign him/her to player 1 or 2 (pass 1)
	void OnCalibrationSuccessBeforePose(uint UserId)
	{
        Debug.Log(String.Format("[{0}] Before calibration success", UserId));
		
		if(!alCalibratedPlayerID.Contains(UserId))
		{
			alCalibratedPlayerID.Add(UserId);
		}
	}
	
	// If a user successfully calibrates, assign him/her to player 1 or 2 (pass 2)
    bool OnCalibrationSuccessAfterPose(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration success", UserId));
		bool bSuccess = false;
		
		// If player 1 hasn't been calibrated, assign that UserID to it.
		if(!Player1Calibrated)
		{
			// Check to make sure we don't accidentally assign player 2 to player 1.
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player1CalibrationPose, ref player1CalibrationData))
				{
					Player1Calibrated = true;
					Player1ID = UserId;
					
					allUsers.Add(UserId);
					bSuccess = true;
					
					foreach(AvatarController controller in Player1Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
					
					// add the gestures to detect, if any
					foreach(KinectGestures.Gestures gesture in Player1Gestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// notify the gesture listeners about the new user
					foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
					{
						listener.UserDetected(UserId, 0);
					}
					
					// If we're not using 2 users, we're all calibrated.
					//if(!TwoUsers)
					{
						AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // true;
					}
				}
			}
		}
		else if(TwoUsers && !Player2Calibrated)
		{
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player2CalibrationPose, ref player2CalibrationData))
				{
					Player2Calibrated = true;
					Player2ID = UserId;
					
					allUsers.Add(UserId);
					bSuccess = true;
					
					foreach(AvatarController controller in Player2Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
					
					// add the gestures to detect, if any
					foreach(KinectGestures.Gestures gesture in Player2Gestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// notify the gesture listeners about the new user
					foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
					{
						listener.UserDetected(UserId, 1);
					}
					
					// All users are calibrated!
					AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // true;
				}
			}
		}
		
		// If all users are calibrated, stop trying to find them.
		if(AllPlayersCalibrated)
		{
			Debug.Log("All players calibrated.");
			
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = "";
			}
			
			KinectWrapper.StopLookingForUsers();
		}
		
		return bSuccess;
    }
	
	// If a user walks out of the kinects all-seeing eye, try to reassign them! Or, assign a new user to player 1.
    void OnUserLost(uint UserId)
    {
        Debug.Log(String.Format("[{0}] User lost", UserId));
		
		// If we lose player 1...
		if(UserId == Player1ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player1ID = 0;
			Player1Calibrated = false;
			
			foreach(AvatarController controller in Player1Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
			{
				listener.UserLost(UserId, 0);
			}
			
			player1CalibrationData.userId = 0;
		}
		
		// If we lose player 2...
		if(UserId == Player2ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player2ID = 0;
			Player2Calibrated = false;
			
			foreach(AvatarController controller in Player2Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			foreach(KinectGestures.GestureListenerInterface listener in gestureListeners)
			{
				listener.UserLost(UserId, 1);
			}
			
			player2CalibrationData.userId = 0;
		}
		
		// clear gestures list for this user
		ClearGestures(UserId);

		// remove it from the list of calibrating users
		if(alCalibratedPlayerID.Contains(UserId))
		{
			alCalibratedPlayerID.Remove(UserId);
		}

        // remove from global users list
        allUsers.Remove(UserId);
		AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // false;
		
		if(!AllPlayersCalibrated)
		{
			// Try to replace that user!
			Debug.Log("Starting looking for users");
			//KinectWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
			KinectWrapper.StartLookingForUsers(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = "WAITING FOR USERS";
			}
		}
		else
		{
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = "";
			}
		}
	}
	
	
	// Estimates the current state of the defined gestures
	void CheckForGestures(uint UserId, ref List<KinectGestures.GestureData> playerGestures, ref float gestureTrackingAtTime)
	{
		// check for gestures
		if(Time.realtimeSinceStartup >= gestureTrackingAtTime)
		{
			int listGestureSize = playerGestures.Count;
			float timestampNow = Time.realtimeSinceStartup;
			
			// get joint positions and tracking
			int iAllJointsCount = (int)KinectWrapper.SkeletonJoint.COUNT;
			bool[] playerJointsTracked = new bool[iAllJointsCount];
			Vector3[] playerJointsPos = new Vector3[iAllJointsCount];
			
			int[] aiNeededJointIndexes = KinectGestures.GetNeededJointIndexes();
			int iNeededJointsCount = aiNeededJointIndexes.Length;
			
			for(int i = 0; i < iNeededJointsCount; i++)
			{
				int joint = aiNeededJointIndexes[i];
				
				if(joint >= 0 && KinectWrapper.GetJointPositionConfidence(UserId, joint) >= 0.5f)
				{
					if(KinectWrapper.GetJointPosition(UserId, joint, ref jointPosition))
					{
						playerJointsTracked[joint] = true;
						playerJointsPos[joint] = new Vector3(jointPosition.x * 0.001f, jointPosition.y * 0.001f + SensorHeight, jointPosition.z * 0.001f);
					}
				}
			}
			
			// check for gestures
			for(int g = 0; g < listGestureSize; g++)
			{
				KinectGestures.GestureData gestureData = playerGestures[g];
				
				if((timestampNow >= gestureData.startTrackingAtTime) && 
					!IsConflictingGestureInProgress(gestureData))
				{
					KinectGestures.CheckForGesture(UserId, ref gestureData, Time.realtimeSinceStartup, 
						ref playerJointsPos, ref playerJointsTracked);
					player1Gestures[g] = gestureData;

					if(gestureData.complete)
					{
						gestureTrackingAtTime = timestampNow + MinTimeBetweenGestures;
					}
				}
			}
		}
	}
	
	bool IsConflictingGestureInProgress(KinectGestures.GestureData gestureData)
	{
		foreach(KinectGestures.Gestures gesture in gestureData.checkForGestures)
		{
			int index = GetGestureIndex(gestureData.userId, gesture);
			
			if(index >= 0)
			{
				if(gestureData.userId == Player1ID)
				{
					if(player1Gestures[index].progress > 0f)
						return true;
				}
				else if(gestureData.userId == Player2ID)
				{
					if(player2Gestures[index].progress > 0f)
						return true;
				}
			}
		}
		
		return false;
	}
	
	// return the index of gesture in the list, or -1 if not found
	private int GetGestureIndex(uint UserId, KinectGestures.Gestures gesture)
	{
		if(UserId == Player1ID)
		{
			int listSize = player1Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player1Gestures[i].gesture == gesture)
					return i;
			}
		}
		else if(UserId == Player2ID)
		{
			int listSize = player2Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player2Gestures[i].gesture == gesture)
					return i;
			}
		}
		
		return -1;
	}
	
	// check if the calibration pose is complete for given user
	private bool CheckForCalibrationPose(uint userId, ref KinectGestures.Gestures calibrationGesture, 
		ref KinectGestures.GestureData gestureData)
	{
		if(calibrationGesture == KinectGestures.Gestures.None)
			return true;
		
		// init gesture data if needed
		if(gestureData.userId != userId)
		{
			gestureData.userId = userId;
			gestureData.gesture = calibrationGesture;
			gestureData.state = 0;
			gestureData.joint = 0;
			gestureData.progress = 0f;
			gestureData.complete = false;
			gestureData.cancelled = false;
		}
		
		// get joint positions and tracking
		int iAllJointsCount = (int)KinectWrapper.SkeletonJoint.COUNT;
		bool[] playerJointsTracked = new bool[iAllJointsCount];
		Vector3[] playerJointsPos = new Vector3[iAllJointsCount];
		
		int[] aiNeededJointIndexes = KinectGestures.GetNeededJointIndexes();
		int iNeededJointsCount = aiNeededJointIndexes.Length;
		
		for(int i = 0; i < iNeededJointsCount; i++)
		{
			int joint = aiNeededJointIndexes[i];
			
			if(joint >= 0 && KinectWrapper.GetJointPositionConfidence(userId, joint) >= 0.5f)
			{
				if(KinectWrapper.GetJointPosition(userId, joint, ref jointPosition))
				{
					playerJointsTracked[joint] = true;
					playerJointsPos[joint] = new Vector3(jointPosition.x * 0.001f, jointPosition.y * 0.001f + SensorHeight, jointPosition.z * 0.001f);
				}
			}
		}
		
		// estimate the gesture progess
		KinectGestures.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
			ref playerJointsPos, ref playerJointsTracked);
		
		// check if gesture is complete
		if(gestureData.complete)
		{
			gestureData.userId = 0;
			return true;
		}
		
		return false;
	}
	
//	// convert the matrix to quaternion, taking care of the mirroring
//	private Quaternion ConvertMatrixToQuat(KinectWrapper.SkeletonJointOrientation ori, int joint, bool flip)
//	{
//		Matrix4x4 mat = Matrix4x4.identity;
//		
//		Quaternion quat = new Quaternion(ori.x, ori.y, ori.z, ori.w);
//		mat.SetTRS(Vector3.zero, quat, Vector3.one);
//
//		Vector3 vZ = mat.GetColumn(2);
//		Vector3 vY = mat.GetColumn(1);
//		
//		if(!flip)
//		{
//			vZ.y = -vZ.y;
//			vY.x = -vY.x;
//			vY.z = -vY.z;
//		}
//		else
//		{
//			vZ.x = -vZ.x;
//			vZ.y = -vZ.y;
//			vY.z = -vY.z;
//		}
//
//		if(vZ.x != 0.0f || vZ.y != 0.0f || vZ.z != 0.0f)
//		{
//			return Quaternion.LookRotation(vZ, vY);
//		}
//
//		return Quaternion.identity;
//	}
	
}


