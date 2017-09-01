using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 

// Wrapper class that holds the various structs, variables, and dll imports
// needed to set up a model with the Kinect.
public class KinectWrapper
{

	private const string MacOpenNI2RedistPath = "/Library/OpenNI-MacOSX-x64-2.2/Redist";
	private const string MacNiTE2RedistPath = "/Library/NiTE-MacOSX-x64-2.2/Redist";


	public static class Constants
	{
		public const int SkeletonCount = 6;
		
		public const float MinTimeBetweenSameGestures = 0.0f;
		public const float PoseCompleteDuration = 1.0f;
		public const float ClickStayDuration = 2.5f;
	}
	
	// Kinect-given Variables to keep track of the skeleton's joints.
	public enum SkeletonJoint
	{ 
		HEAD = 0,
        NECK = 1,

		LEFT_SHOULDER = 2,
		RIGHT_SHOULDER = 3,
		LEFT_ELBOW = 4,
		RIGHT_ELBOW = 5,
		LEFT_HAND = 6,
		RIGHT_HAND = 7,

		HIPS = 8,

		LEFT_HIP = 9,
		RIGHT_HIP = 10,
		LEFT_KNEE = 11,
		RIGHT_KNEE = 12,
		LEFT_FOOT = 13,
		RIGHT_FOOT = 14,

		COUNT 
	};
	
	// Struct to store color RGB888
	public struct ColorRgb888
	{
		public byte r;
		public byte g;
		public byte b;
	}
	
	// Struct to store the joint's poision.
    public struct SkeletonJointPosition
    {
        public float x, y, z;
    }
	
	// Struct that will hold the joints orientation.
    public struct SkeletonJointOrientation
    {
        public float x, y, z, w;
    }
	
	// Struct that combines the previous two and makes the transform.
    public struct SkeletonJointTransformation
    {
		public SkeletonJoint jointType;
        public SkeletonJointPosition position;
        public float positionConfidence;
        public SkeletonJointOrientation orientation;
        public float orientationConfidence;
    }
	
	// DLL Imports to pull in the necessary Unity functions to make the Kinect go.
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int Init(bool isInitDepthStream, bool isInitColorStream, bool isInitInfraredStream);
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern void Shutdown();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int Update([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = Constants.SkeletonCount, ArraySubType = UnmanagedType.U2)] short[] pUsers,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = Constants.SkeletonCount, ArraySubType = UnmanagedType.U2)] short[] pStates, ref int pUsersCount);
	
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern IntPtr GetLastErrorString();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetDepthWidth();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetDepthHeight();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetInfraredWidth();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetInfraredHeight();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetColorWidth();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern int GetColorHeight();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern IntPtr GetUsersLabelMap();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern IntPtr GetUsersDepthMap();
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern IntPtr GetUsersInfraredMap();
	[DllImport("UnityInterface2", SetLastError=true)]
    public static extern IntPtr GetUsersColorMap();

	[DllImport("UnityInterface2", SetLastError=true)]
    public static extern void SetSkeletonSmoothing(float factor);

	[DllImport("UnityInterface2", SetLastError=true, EntryPoint="GetJointTransformation")]
	public static extern bool GetJointTransformationNative(uint userID, int joint, ref SkeletonJointTransformation pTransformation);
	[DllImport("UnityInterface2", SetLastError=true, EntryPoint="GetJointPosition")]
	public static extern bool GetJointPositionNative(uint userID, int joint, ref SkeletonJointPosition pPosition);
	[DllImport("UnityInterface2", SetLastError=true, EntryPoint="GetJointOrientation")]
	public static extern bool GetJointOrientationNative(uint userID, int joint, ref SkeletonJointOrientation pOrientation);
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern float GetJointPositionConfidence(uint userID, int joint);
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern float GetJointOrientationConfidence(uint userID, int joint);
	
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern void StartLookingForUsers(IntPtr NewUser, IntPtr CalibrationStarted, IntPtr CalibrationFailed, IntPtr CalibrationSuccess, IntPtr UserLost);
	[DllImport("UnityInterface2", SetLastError=true)]
	public static extern void StopLookingForUsers();

    public delegate void UserDelegate(uint userId);


    public static void StartLookingForUsers(UserDelegate NewUser, UserDelegate CalibrationStarted, UserDelegate CalibrationFailed, UserDelegate CalibrationSuccess, UserDelegate UserLost)
    {
        StartLookingForUsers(
            Marshal.GetFunctionPointerForDelegate(NewUser),
            Marshal.GetFunctionPointerForDelegate(CalibrationStarted),
            Marshal.GetFunctionPointerForDelegate(CalibrationFailed),
            Marshal.GetFunctionPointerForDelegate(CalibrationSuccess),
            Marshal.GetFunctionPointerForDelegate(UserLost)
		);
    }

	public static bool GetJointPosition(uint userID, int joint, ref SkeletonJointPosition pPosition)
	{
		if (GetJointPositionNative (userID, joint, ref pPosition)) 
		{
			//pPosition.x = -pPosition.x;
			return true;
		}

		return false;
	}
	
	public static bool GetJointOrientation(uint userID, int joint, ref SkeletonJointOrientation pOrientation)
	{
		if (GetJointOrientationNative (userID, joint, ref pOrientation)) 
		{
			return true;
		}
		
		return false;
	}
	
	public static bool GetSkeletonJointOrientation(uint userID, int joint, bool flip, ref Quaternion pJointRot)
	{
		Matrix4x4 matOri = Matrix4x4.identity;
		bool bMatHasOri = false;
		
		if(joint == (int)SkeletonJoint.LEFT_HAND)
		{
			// special case - left hand
			SkeletonJointPosition posElbow = new SkeletonJointPosition();
			SkeletonJointPosition posHand = new SkeletonJointPosition();
			SkeletonJointPosition posHips = new SkeletonJointPosition();
			SkeletonJointPosition posNeck = new SkeletonJointPosition();
			
			bool bElbowTracked = GetJointPosition(userID, (int)SkeletonJoint.LEFT_ELBOW, ref posElbow);
			bool bHandTracked = GetJointPosition(userID, (int)SkeletonJoint.LEFT_HAND, ref posHand);
			bool bHipsTracked = GetJointPosition(userID, (int)SkeletonJoint.HIPS, ref posHips);
			bool bNeckTracked = GetJointPosition(userID, (int)SkeletonJoint.NECK, ref posNeck);
			
			if(bElbowTracked && bHandTracked && bHipsTracked && bNeckTracked)
			{
				Vector3 vElbow = new Vector3(posElbow.x, posElbow.y, posElbow.z);
				Vector3 vHand = new Vector3(posHand.x, posHand.y, posHand.z);
				Vector3 vHips = new Vector3(posHips.x, posHips.y, posHips.z);
				Vector3 vNeck = new Vector3(posNeck.x, posNeck.y, posNeck.z);
				
				Vector3 vx = -(vHand - vElbow);
				Vector3 vy = vNeck - vHips;
				
				MakeMatrixFromXY(vx, vy, ref matOri);
				bMatHasOri = true;
			}
		}
		else if(joint == (int)SkeletonJoint.RIGHT_HAND)
		{
			// special case - right hand
			SkeletonJointPosition posElbow = new SkeletonJointPosition();
			SkeletonJointPosition posHand = new SkeletonJointPosition();
			SkeletonJointPosition posHips = new SkeletonJointPosition();
			SkeletonJointPosition posNeck = new SkeletonJointPosition();
			
			bool bElbowTracked = GetJointPosition(userID, (int)SkeletonJoint.RIGHT_ELBOW, ref posElbow);
			bool bHandTracked = GetJointPosition(userID, (int)SkeletonJoint.RIGHT_HAND, ref posHand);
			bool bHipsTracked = GetJointPosition(userID, (int)SkeletonJoint.HIPS, ref posHips);
			bool bNeckTracked = GetJointPosition(userID, (int)SkeletonJoint.NECK, ref posNeck);
			
			if(bElbowTracked && bHandTracked && bHipsTracked && bNeckTracked)
			{
				Vector3 vElbow = new Vector3(posElbow.x, posElbow.y, posElbow.z);
				Vector3 vHand = new Vector3(posHand.x, posHand.y, posHand.z);
				Vector3 vHips = new Vector3(posHips.x, posHips.y, posHips.z);
				Vector3 vNeck = new Vector3(posNeck.x, posNeck.y, posNeck.z);
				
				Vector3 vx = vHand - vElbow;
				Vector3 vy = vNeck - vHips;
				
				MakeMatrixFromXY(vx, vy, ref matOri);
				bMatHasOri = true;
			}
		}
		else
		{
			// all other joints
			SkeletonJointOrientation oriJoint = new SkeletonJointOrientation();
			
			if(GetJointOrientation(userID, joint, ref oriJoint))
			{
				Quaternion rotJoint = new Quaternion(oriJoint.x, oriJoint.y, oriJoint.z, oriJoint.w);
				matOri.SetTRS(Vector3.zero, rotJoint, Vector3.one);
				bMatHasOri = true;
			}
		}
		
		if(bMatHasOri)
		{
			Vector4 vZ = matOri.GetColumn(2);
			Vector4 vY = matOri.GetColumn(1);

			if(!flip)
			{
				vZ.y = -vZ.y;
				vY.x = -vY.x;
				vY.z = -vY.z;
			}
			else
			{
				vZ.x = -vZ.x;
				vZ.y = -vZ.y;
				vY.z = -vY.z;
			}
	
			if(vZ.x != 0.0f || vZ.y != 0.0f || vZ.z != 0.0f)
				pJointRot = Quaternion.LookRotation(vZ, vY);
			else
				bMatHasOri = false;
		}
		
		return bMatHasOri;
	}
	
    //constructs an orientation from 2 vectors: the first specifies the x axis, and the next specifies the y axis
    //uses the first vector as x axis, then constructs the other axes using cross products
    private static void MakeMatrixFromXY(Vector3 xUnnormalized, Vector3 yUnnormalized, ref Matrix4x4 jointOrientation) 
	{
        //matrix columns
        Vector3 xCol;
        Vector3 yCol;
        Vector3 zCol;

        //set up the three different columns to be rearranged and flipped
        xCol = xUnnormalized.normalized;
        zCol = Vector3.Cross(xCol, yUnnormalized.normalized).normalized;
        yCol = Vector3.Cross(zCol, xCol);
		//yCol = yUnnormalized.normalized;
		//zCol = Vector3.Cross(xCol, yCol).normalized;

        //copy values into matrix
        PopulateMatrix(ref jointOrientation, xCol, yCol, zCol);
    }
   
    //populate matrix using the columns
    private static void PopulateMatrix(ref Matrix4x4 jointOrientation, Vector3 xCol, Vector3 yCol, Vector3 zCol) 
	{
    	jointOrientation.SetColumn(0, xCol);
    	jointOrientation.SetColumn(1, yCol);
    	jointOrientation.SetColumn(2, zCol);
    }

	public static int GetSkeletonMirroredJoint(int jointIndex)
	{
		switch(jointIndex)
		{
			case (int)SkeletonJoint.LEFT_SHOULDER:
				return (int)SkeletonJoint.RIGHT_SHOULDER;
			case (int)SkeletonJoint.LEFT_ELBOW:
				return (int)SkeletonJoint.RIGHT_ELBOW;
			case (int)SkeletonJoint.LEFT_HAND:
				return (int)SkeletonJoint.RIGHT_HAND;
			case (int)SkeletonJoint.RIGHT_SHOULDER:
				return (int)SkeletonJoint.LEFT_SHOULDER;
			case (int)SkeletonJoint.RIGHT_ELBOW:
				return (int)SkeletonJoint.LEFT_ELBOW;
			case (int)SkeletonJoint.RIGHT_HAND:
				return (int)SkeletonJoint.LEFT_HAND;
			case (int)SkeletonJoint.LEFT_HIP:
				return (int)SkeletonJoint.RIGHT_HIP;
			case (int)SkeletonJoint.LEFT_KNEE:
				return (int)SkeletonJoint.RIGHT_KNEE;
			case (int)SkeletonJoint.LEFT_FOOT:
				return (int)SkeletonJoint.RIGHT_FOOT;
			case (int)SkeletonJoint.RIGHT_HIP:
				return (int)SkeletonJoint.LEFT_HIP;
			case (int)SkeletonJoint.RIGHT_KNEE:
				return (int)SkeletonJoint.LEFT_KNEE;
			case (int)SkeletonJoint.RIGHT_FOOT:
				return (int)SkeletonJoint.LEFT_FOOT;
		}
		
		return jointIndex;
	}

	// copies and configures the needed resources in the project directory
	public static bool CheckOpenNIPresence()
	{
		bool bOneCopied = false, bAllCopied = true;
		bool bArchX64 = Is64bitArchitecture();

		// check openni directory and resources
		string sOpenNIPath = System.Environment.GetEnvironmentVariable(!bArchX64 ? "OPENNI2_REDIST" : "OPENNI2_REDIST64");
		if(sOpenNIPath == string.Empty || !Directory.Exists(sOpenNIPath))
			sOpenNIPath = MacOpenNI2RedistPath;  // take the constant path
		if(sOpenNIPath == String.Empty || !Directory.Exists(sOpenNIPath))
			throw new Exception("OpenNI2 directory not found. Please check the OpenNI installation.");
		
		sOpenNIPath = sOpenNIPath.Replace('\\', '/');
		if(sOpenNIPath.EndsWith("/"))
			sOpenNIPath = sOpenNIPath.Substring(0, sOpenNIPath.Length - 1);
		
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if(!File.Exists("OpenNI2.dll"))
		{
			string srcOpenNiDll = sOpenNIPath + "/OpenNI2.dll";

			if(File.Exists(srcOpenNiDll))
			{
				Debug.Log("Copying OpenNI2 library...");
				File.Copy(srcOpenNiDll, "OpenNI2.dll");
					
				bOneCopied = File.Exists("OpenNI2.dll");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied OpenNI2 library.");
			}
		}
#endif
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		if(!File.Exists("libOpenNI2.dylib"))
		{
			string srcOpenNiDll = sOpenNIPath + "/libOpenNI2.dylib";

			if(File.Exists(srcOpenNiDll))
			{
				Debug.Log("Copying OpenNI2 library...");
				File.Copy(srcOpenNiDll, "libOpenNI2.dylib");
					
				bOneCopied = File.Exists("libOpenNI2.dylib");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied OpenNI2 library.");
			}
		}
#endif
		
		if(!File.Exists("OpenNI.ini"))
		{
			Debug.Log("Copying OpenNI2 configuration...");
			TextAsset textRes = Resources.Load("OpenNI.ini", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				string sResText = textRes.text.Replace("%OPENNI_REDIST_DIR%", sOpenNIPath);
				File.WriteAllText("OpenNI.ini", sResText);
				
				bOneCopied = File.Exists("OpenNI.ini");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied OpenNI2 configuration.");
			}
		}

		// check nite directory and resources
		string sNiTEPath = System.Environment.GetEnvironmentVariable(!bArchX64 ? "NITE2_REDIST" : "NITE2_REDIST64");
		if(sNiTEPath == string.Empty || !Directory.Exists(sNiTEPath))
			sNiTEPath = MacNiTE2RedistPath;  // take the constant path

		if(sNiTEPath == String.Empty || !Directory.Exists(sNiTEPath))
			throw new Exception("NiTE2 directory not found. Please check the NiTE installation.");
		
		sNiTEPath = sNiTEPath.Replace('\\', '/');
		if(sNiTEPath.EndsWith("/"))
			sNiTEPath = sNiTEPath.Substring(0, sNiTEPath.Length - 1);
		
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if(!File.Exists("NiTE2.dll"))
		{
			string srcNiteDll = sNiTEPath + "/NiTE2.dll";

			if(File.Exists(srcNiteDll))
			{
				Debug.Log("Copying NiTE2 library...");
				File.Copy(srcNiteDll, "NiTE2.dll");
					
				bOneCopied = File.Exists("NiTE2.dll");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied NITE2 library.");
			}
		}
#endif
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		if(!File.Exists("libNiTE2.dylib"))
		{
			string srcNiteDll = sNiTEPath + "/libNiTE2.dylib";

			if(File.Exists(srcNiteDll))
			{
				Debug.Log("Copying NiTE2 library...");
				File.Copy(srcNiteDll, "libNiTE2.dylib");
					
				bOneCopied = File.Exists("libNiTE2.dylib");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied NITE2 library.");
			}
		}
#endif
		
		if(!File.Exists("NiTE.ini"))
		{
			Debug.Log("Copying NiTE2 configuration...");
			TextAsset textRes = Resources.Load("NiTE.ini", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				string sResText = textRes.text.Replace("%NITE_REDIST_DIR%", sNiTEPath);
				File.WriteAllText("NiTE.ini", sResText);
				
				bOneCopied = File.Exists("NiTE.ini");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied NITE2 configuration.");
			}
		}

		// check the unity interface library
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if(!File.Exists("UnityInterface2.dll"))
		{
			Debug.Log("Copying UnityInterface2 library...");
			TextAsset textRes = Resources.Load(!bArchX64 ? "UnityInterface2.x86.dll" : "UnityInterface2.x64.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				File.WriteAllBytes("UnityInterface2.dll", textRes.bytes);
				
				bOneCopied = File.Exists("UnityInterface2.dll");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied UnityInterface library.");
			}
		}

		if(!File.Exists("msvcp100.dll"))
		{
			Debug.Log("Copying msvcp90d library...");
			TextAsset textRes = Resources.Load(!bArchX64 ? "msvcp100.x86.dll" : "msvcp100.x64.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcp100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcp100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcp100 library.");
			}
		}
		
		if(!File.Exists("msvcr100.dll"))
		{
			Debug.Log("Copying msvcr90d library...");
			TextAsset textRes = Resources.Load(!bArchX64 ? "msvcr100.x86.dll" : "msvcr100.x64.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcr100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcr100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcr100 library.");
			}
		}
		
#endif
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		if(!File.Exists("libUnityInterface2.dylib"))
		{
			Debug.Log("Copying UnityInterface2 library...");
			TextAsset textRes = Resources.Load("libUnityInterface2.dylib", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				File.WriteAllBytes("libUnityInterface2.dylib", textRes.bytes);
				
				bOneCopied = File.Exists("libUnityInterface2.dylib");
				bAllCopied = bAllCopied && bOneCopied;
					
				if(bOneCopied)
					Debug.Log("Copied UnityInterface library.");
			}
		}
#endif
		
		return bOneCopied && bAllCopied;
	}
	
	// returns true if the project is running on 64-bit architecture, false if 32-bit
	public static bool Is64bitArchitecture()
	{
		int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
		return (sizeOfPtr > 4);
	}

}

