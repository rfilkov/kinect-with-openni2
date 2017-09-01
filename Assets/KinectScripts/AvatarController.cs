using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 

public class AvatarController : MonoBehaviour
{	
	// Bool that determines whether the avatar is active.
	//public bool Active = true;
	
	// Bool that has the characters (facing the player) actions become mirrored. Default false.
	public bool MirroredMovement = false;
	
	// Bool that determines whether the avatar will move or not in space.
	// bool MovesInSpace = true;
	
	// Bool that determines whether the avatar is allowed to jump -- vertical movement
	// can cause some models to behave strangely, so use at your own discretion.
	public bool VerticalMovement = false;
	
	// Rate at which avatar will move through the scene. The rate multiplies the movement speed (.001f, i.e dividing by 1000, unity's framerate).
	private int MoveRate = 1;
	
	// Slerp smooth factor
	public float SmoothFactor = 5f;
	
	// Public variables that will get matched to bones. If empty, the kinect will simply not track it.
	// These bones can be set within the Unity interface.
	public Transform Hips;
	//public Transform Spine;
	public Transform Neck;
	public Transform Head;
	
	//public Transform LeftShoulder;
	public Transform LeftUpperArm;
	public Transform LeftElbow; 
	//public Transform LeftWrist;
	public Transform LeftHand;
	//public Transform LeftFingers;
	
	//public Transform RightShoulder;
	public Transform RightUpperArm;
	public Transform RightElbow;
	//public Transform RightWrist;
	public Transform RightHand;
	//public Transform RightFingers;
	
	public Transform LeftThigh;
	public Transform LeftKnee;
	public Transform LeftFoot;
	//public Transform LeftToes;
	
	public Transform RightThigh;
	public Transform RightKnee;
	public Transform RightFoot;
	//public Transform RightToes;
	
	public Transform Root;
	
	// A required variable if you want to rotate the model in space.
	public GameObject offsetNode;
	
	// Variable to hold all them bones. It will initialize the same size as initialRotations.
	private Transform[] bones;
	
	// Rotations of the bones when the Kinect tracking starts.
    private Quaternion[] initialRotations;
	
	// Calibration Offset Variables for Character Position.
	bool OffsetCalibrated = false;
	float XOffset, YOffset, ZOffset;
	Quaternion originalRotation;
	
	
    public void Start()
    {	
		// Holds our bones for later.
		bones = new Transform[(int)KinectWrapper.SkeletonJoint.COUNT];
		
		// Initial rotations of said bones.
		initialRotations = new Quaternion[bones.Length];
		
		// Map bones to the points the Kinect tracks.
		MapBones();

		// Get initial rotations to return to later.
		GetInitialRotations();
		
		// Set the model to the calibration pose.
        RotateToCalibrationPose(0, KinectManager.IsCalibrationNeeded());
    }
	
	// Update the avatar each frame.
    public void UpdateAvatar(uint UserID, bool IsNearMode)
    {	
		TransformBone(UserID, KinectWrapper.SkeletonJoint.HIPS, MirroredMovement);
		//TransformBone(UserID, KinectWrapper.SkeletonJoint.SPINE, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.NECK, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.HEAD, MirroredMovement);
		
		TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_SHOULDER, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_ELBOW, MirroredMovement);
		//TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_WRIST, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_HAND, MirroredMovement);
		
		TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_SHOULDER, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_ELBOW, MirroredMovement);
		//TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_WRIST, MirroredMovement);
		TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_HAND, MirroredMovement);
		
		if(!IsNearMode)
		{
			TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_HIP, MirroredMovement);
			TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_KNEE, MirroredMovement);
			//TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_ANKLE, MirroredMovement);
			TransformBone(UserID, KinectWrapper.SkeletonJoint.LEFT_FOOT, MirroredMovement);
			
			TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_HIP, MirroredMovement);
			TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_KNEE, MirroredMovement);
			//TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_ANKLE, MirroredMovement);
			TransformBone(UserID, KinectWrapper.SkeletonJoint.RIGHT_FOOT, MirroredMovement);	
		}
		
		// If the avatar is supposed to move in the space, move it.
		//if (MovesInSpace)
		{
			MoveAvatar(UserID);
		}
    }
	
	// Calibration pose is simply initial position with hands raised up. Rotation must be 0,0,0 to calibrate.
    public void RotateToCalibrationPose(uint userId, bool needCalibration)
    {	
		// Reset the rest of the model to the original position.
        RotateToInitialPosition();
		
		if(needCalibration)
		{
			if(offsetNode != null)
			{
				// Set the offset's rotation to 0.
				offsetNode.transform.rotation = Quaternion.Euler(Vector3.zero);
			}
			
//			// Right Elbow
//			if(RightElbow != null)
//	        	RightElbow.rotation = Quaternion.Euler(0, -90, 90) * 
//					initialRotations[(int)KinectWrapper.SkeletonJoint.RIGHT_ELBOW];
//			
//			// Left Elbow
//			if(LeftElbow != null)
//	        	LeftElbow.rotation = Quaternion.Euler(0, 90, -90) * 
//					initialRotations[(int)KinectWrapper.SkeletonJoint.LEFT_ELBOW];

			if(offsetNode != null)
			{
				// Restore the offset's rotation
				offsetNode.transform.rotation = originalRotation;
			}
		}
    }
	
	// Invoked on the successful calibration of a player.
	public void SuccessfulCalibration(uint userId)
	{
		// reset the models position
		if(offsetNode != null)
		{
			offsetNode.transform.rotation = originalRotation;
		}
		
		// re-calibrate the position offset
		OffsetCalibrated = false;
	}
	
	// Returns the correct bone index, depending on the mirroring
	int GetJointIndex(KinectWrapper.SkeletonJoint joint, bool isMirrored)
	{
		if(isMirrored)
		{
			switch(joint)
			{
				case KinectWrapper.SkeletonJoint.LEFT_SHOULDER:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_SHOULDER;
				case KinectWrapper.SkeletonJoint.LEFT_ELBOW:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_ELBOW;
				case KinectWrapper.SkeletonJoint.LEFT_HAND:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_HAND;
				case KinectWrapper.SkeletonJoint.RIGHT_SHOULDER:
					return (int)KinectWrapper.SkeletonJoint.LEFT_SHOULDER;
				case KinectWrapper.SkeletonJoint.RIGHT_ELBOW:
					return (int)KinectWrapper.SkeletonJoint.LEFT_ELBOW;
				case KinectWrapper.SkeletonJoint.RIGHT_HAND:
					return (int)KinectWrapper.SkeletonJoint.LEFT_HAND;
				case KinectWrapper.SkeletonJoint.LEFT_HIP:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_HIP;
				case KinectWrapper.SkeletonJoint.LEFT_KNEE:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_KNEE;
				case KinectWrapper.SkeletonJoint.LEFT_FOOT:
					return (int)KinectWrapper.SkeletonJoint.RIGHT_FOOT;
				case KinectWrapper.SkeletonJoint.RIGHT_HIP:
					return (int)KinectWrapper.SkeletonJoint.LEFT_HIP;
				case KinectWrapper.SkeletonJoint.RIGHT_KNEE:
					return (int)KinectWrapper.SkeletonJoint.LEFT_KNEE;
				case KinectWrapper.SkeletonJoint.RIGHT_FOOT:
					return (int)KinectWrapper.SkeletonJoint.LEFT_FOOT;
			}
		}
		
		return (int)joint;
	}

	// Apply the rotations tracked by kinect to the joints.
    void TransformBone(uint userId, KinectWrapper.SkeletonJoint joint, bool isMirrored)
    {
		int boneIndex = GetJointIndex(joint, isMirrored);
		if(boneIndex < 0)
			return;
		
		Transform boneTransform = bones[boneIndex];
		if(boneTransform == null)
			return;
		
		// Grab the bone we're moving.
		int iJoint = (int)joint;
		if(iJoint < 0)
			return;
		
		// Get Kinect joint orientation
		Quaternion jointRotation = KinectManager.Instance.GetJointOrientation(userId, iJoint, !isMirrored);
		if(jointRotation == Quaternion.identity)
			return;
		
		// Apply the new rotation.
        Quaternion newRotation = jointRotation * initialRotations[boneIndex];
		
		//If an offset node is specified, combine the transform with its
		//orientation to essentially make the skeleton relative to the node
		if (offsetNode != null)
		{
			// Grab the total rotation by adding the Euler and offset's Euler.
			Vector3 totalRotation = newRotation.eulerAngles + offsetNode.transform.rotation.eulerAngles;
			// Grab our new rotation.
			newRotation = Quaternion.Euler(totalRotation);
		}
		
		// Smoothly transition to our new rotation.
        boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, Time.deltaTime * SmoothFactor);
	}
	
	// Moves the avatar in 3D space - pulls the tracked position of the spine and applies it to root.
	// Only pulls positional, not rotational.
	void MoveAvatar(uint UserID)
	{
		if(Root == null)
			return;
		if(!KinectManager.Instance.IsJointPositionTracked(UserID, (int)KinectWrapper.SkeletonJoint.HIPS))
			return;
		
        // Get the position of the body and store it.
		Vector3 trans = KinectManager.Instance.GetUserPosition(UserID);
		
		// If this is the first time we're moving the avatar, set the offset. Otherwise ignore it.
		if (!OffsetCalibrated)
		{
			OffsetCalibrated = true;
			
			XOffset = !MirroredMovement ? trans.x * MoveRate : -trans.x * MoveRate;
			YOffset = trans.y * MoveRate;
			ZOffset = -trans.z * MoveRate;
		}
	
		float xPos;
		float yPos;
		float zPos;
		
		// If movement is mirrored, reverse it.
		if(!MirroredMovement)
			xPos = trans.x * MoveRate - XOffset;
		else
			xPos = -trans.x * MoveRate - XOffset;
		
		yPos = trans.y * MoveRate - YOffset;
		zPos = -trans.z * MoveRate - ZOffset;
		
		// If we are tracking vertical movement, update the y. Otherwise leave it alone.
		Vector3 targetPos = new Vector3(xPos, VerticalMovement ? yPos : 0f, zPos);
		Root.localPosition = Vector3.Lerp(Root.localPosition, targetPos, 3 * Time.deltaTime);
	}
	
	// If the bones to be mapped have been declared, map that bone to the model.
	void MapBones()
	{
		// If they're not empty, pull in the values from Unity and assign them to the array.
		if(Hips != null && (int)KinectWrapper.SkeletonJoint.HIPS >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.HIPS] = Hips;
//		if(Spine != null && (int)KinectWrapper.SkeletonJoint.SPINE >= 0)
//			bones[(int)KinectWrapper.SkeletonJoint.SPINE] = Spine;
		if(Neck != null && (int)KinectWrapper.SkeletonJoint.NECK >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.NECK] = Neck;
		if(Head != null && (int)KinectWrapper.SkeletonJoint.HEAD >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.HEAD] = Head;
		
		if(LeftUpperArm != null && (int)KinectWrapper.SkeletonJoint.LEFT_SHOULDER >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_SHOULDER] = LeftUpperArm;
		if(LeftElbow != null && (int)KinectWrapper.SkeletonJoint.LEFT_ELBOW >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_ELBOW] = LeftElbow;
//		if(LeftWrist != null && (int)KinectWrapper.SkeletonJoint.LEFT_WRIST >= 0)
//			bones[(int)KinectWrapper.SkeletonJoint.LEFT_WRIST] = LeftWrist;
		if(LeftHand != null && (int)KinectWrapper.SkeletonJoint.LEFT_HAND >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_HAND] = LeftHand;
		
		if(RightUpperArm != null && (int)KinectWrapper.SkeletonJoint.RIGHT_SHOULDER >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_SHOULDER] = RightUpperArm;
		if(RightElbow != null && (int)KinectWrapper.SkeletonJoint.RIGHT_ELBOW >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_ELBOW] = RightElbow;
//		if(RightWrist != null && (int)KinectWrapper.SkeletonJoint.RIGHT_WRIST >= 0)
//			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_WRIST] = RightWrist;
		if(RightHand != null && (int)KinectWrapper.SkeletonJoint.RIGHT_HAND >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_HAND] = RightHand;
		
		if(LeftThigh != null && (int)KinectWrapper.SkeletonJoint.LEFT_HIP >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_HIP] = LeftThigh;
		if(LeftKnee != null && (int)KinectWrapper.SkeletonJoint.LEFT_KNEE >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_KNEE] = LeftKnee;
//		if(LeftFoot != null && (int)KinectWrapper.SkeletonJoint.LEFT_ANKLE >= 0)
//			bones[(int)KinectWrapper.SkeletonJoint.LEFT_ANKLE] = LeftFoot;
		if(LeftFoot != null && (int)KinectWrapper.SkeletonJoint.LEFT_FOOT >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.LEFT_FOOT] = LeftFoot;
		
		if(RightThigh != null && (int)KinectWrapper.SkeletonJoint.RIGHT_HIP >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_HIP] = RightThigh;
		if(RightKnee != null && (int)KinectWrapper.SkeletonJoint.RIGHT_KNEE >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_KNEE] = RightKnee;
//		if(RightFoot != null && (int)KinectWrapper.SkeletonJoint.RIGHT_ANKLE >= 0)
//			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_ANKLE] = RightFoot;
		if(RightFoot!= null && (int)KinectWrapper.SkeletonJoint.RIGHT_FOOT >= 0)
			bones[(int)KinectWrapper.SkeletonJoint.RIGHT_FOOT] = RightFoot;
	}
	
	// Capture the initial rotations of the model.
	void GetInitialRotations()
	{
		if(offsetNode != null)
		{
			// Store the original offset's rotation.
			originalRotation = offsetNode.transform.rotation;
			// Set the offset's rotation to 0.
			offsetNode.transform.rotation = Quaternion.Euler(Vector3.zero);
		}
		
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				initialRotations[i] = bones[i].rotation;
			}
		}

		if(offsetNode != null)
		{
			// Restore the offset's rotation
			offsetNode.transform.rotation = originalRotation;
		}
	}

	// Set bones to initial position.
    public void RotateToInitialPosition()
    {	
		if(bones == null)
			return;
		
		if(offsetNode != null)
		{
			// Set the offset's rotation to 0.
			offsetNode.transform.rotation = Quaternion.Euler(Vector3.zero);
		}
		
		// For each bone that was defined, reset to initial position.
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				bones[i].rotation = initialRotations[i];
			}
		}

		if(Root != null)
		{
			Root.localPosition = Vector3.zero;
		}

		if(offsetNode != null)
		{
			// Restore the offset's rotation
			offsetNode.transform.rotation = originalRotation;
		}
    }
	
}