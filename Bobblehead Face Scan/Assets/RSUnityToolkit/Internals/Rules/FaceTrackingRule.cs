/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2012-2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/

using UnityEngine;
using System.Collections;

namespace RSUnityToolkit
{
    /// <summary>
    /// Face tracking rule - Process Track triggers
    /// </summary>
    [TrackTrigger.TrackTriggerAtt]
    [AddComponentMenu("")]
	public class FaceTrackingRule : BaseRule
    {
        #region Public Fields

        /// <summary>
        /// The landmark to track. (in the group)
        /// </summary>
        public PXCMFaceData.LandmarkType LandmarkToTrack = PXCMFaceData.LandmarkType.LANDMARK_NOSE_TIP;

        public bool useBoundingBox = false;

        /// <summary>
        /// The real world box center. In centimiters.
        /// </summary>
        public Vector3 RealWorldBoxCenter = new Vector3(0, 0, 50);

        /// <summary>
        /// The real world box dimensions. In Centimiters.
        /// </summary>
        public Vector3 RealWorldBoxDimensions = new Vector3(100, 100, 100);
		
		/// <summary>
		/// The index of the face.
		/// </summary>
		public int FaceIndex = 0;
		
        #endregion

        #region Private Fields

        private PXCMPoint3DF32[] _pos_ijz;
        private PXCMPoint3DF32[] _pos3d;

        #endregion

        #region C'tors
        public FaceTrackingRule(): base()
        {
            FriendlyName = "Face Tracking";
        }
        #endregion

        #region Public Methods

        override public string GetIconPath()
        {
            return @"RulesIcons/face-tracking";
        }

        protected override bool OnRuleEnabled()
        {
            SenseToolkitManager.Instance.SetSenseOption(SenseOption.SenseOptionID.Face);
            return true;
        }
		
		protected override void OnRuleDisabled()
		{
			SenseToolkitManager.Instance.UnsetSenseOption(SenseOption.SenseOptionID.Face);
		}

        override public string GetRuleDescription()
        {
            return "Tracks face landmark's position and orientation";
        }

        public override bool Process(Trigger trigger)
        {
            trigger.ErrorDetected = false;

            if (!SenseToolkitManager.Instance.IsSenseOptionSet(SenseOption.SenseOptionID.Face))
            {
                trigger.ErrorDetected = true;
                return false;
            }

            if (!(trigger is TrackTrigger))
            {
                trigger.ErrorDetected = true;
                return false;
            }

            // make sure we have valid values
            if (RealWorldBoxDimensions.x <= 0)
            {
                RealWorldBoxDimensions.x = 1;
            }

            if (RealWorldBoxDimensions.y <= 0)
            {
                RealWorldBoxDimensions.y = 1;
            }

            if (RealWorldBoxDimensions.z <= 0)
            {
                RealWorldBoxDimensions.z = 1;
            }

            if (SenseToolkitManager.Instance.Initialized
                    &&
                    SenseToolkitManager.Instance.FaceModuleOutput != null)
            {

				if (SenseToolkitManager.Instance.FaceModuleOutput.QueryNumberOfDetectedFaces() == 0)
				{
                    ((TrackTrigger)trigger).Position = Vector3.zero;
					return false;					
				}
                
				PXCMFaceData.Face singleFaceOutput = null; 

                singleFaceOutput = SenseToolkitManager.Instance.FaceModuleOutput.QueryFaceByIndex(FaceIndex);

                bool success = false;			
                if (singleFaceOutput != null && singleFaceOutput.QueryUserID() >= 0)
                {
                    // Process Tracking
                    if (trigger is TrackTrigger)
                    {
                        TrackTrigger specificTrigger = (TrackTrigger)trigger;

                        var landmarksData = singleFaceOutput.QueryLandmarks();
                        bool hasLandmarks = false;

                        if (landmarksData != null)
                        {
                            PXCMFaceData.LandmarkPoint outpt = null;
                            bool hasPoint = landmarksData.QueryPoint(landmarksData.QueryPointIndex(LandmarkToTrack), out outpt);
                            if (hasPoint)
                            {
                                hasLandmarks = outpt.confidenceWorld != 0;
                            }
                        }

                        if (!hasLandmarks && useBoundingBox)
                        {
                            PXCMRectI32 rect = new PXCMRectI32();
                            if (singleFaceOutput.QueryDetection() != null && singleFaceOutput.QueryDetection().QueryBoundingRect(out rect))
                            {
                                float depth;
                                singleFaceOutput.QueryDetection().QueryFaceAverageDepth(out depth);
                                float bbCenterX = (rect.x + rect.w / 2);
                                float bbCenterY = (rect.y + rect.h / 2);

                                Vector3 vec = new Vector3();

                                if (_pos_ijz == null)
                                {
                                    _pos_ijz = new PXCMPoint3DF32[1] { new PXCMPoint3DF32() };
                                }
                                _pos_ijz[0].x = bbCenterX;
                                _pos_ijz[0].y = bbCenterY;
                                _pos_ijz[0].z = depth;

                                if (_pos3d == null)
                                {
                                    _pos3d = new PXCMPoint3DF32[1] { new PXCMPoint3DF32() };
                                }

                                SenseToolkitManager.Instance.Projection.ProjectDepthToCamera(_pos_ijz, _pos3d);

                                Vector3 position = new Vector3();
                                vec.x = _pos3d[0].x / 10f;
                                vec.y = _pos3d[0].y / 10f;
                                vec.z = _pos3d[0].z / 10f;

                                // Clamp and normalize to the Real World Box
                                TrackingUtilityClass.ClampToRealWorldInputBox(ref vec, RealWorldBoxCenter, RealWorldBoxDimensions);
                                TrackingUtilityClass.Normalize(ref vec, RealWorldBoxCenter, RealWorldBoxDimensions);

                                if (!float.IsNaN(vec.x) && !float.IsNaN(vec.y) && !float.IsNaN(vec.z))
                                {
                                    specificTrigger.Position = vec;
                                    return true;
                                }
                            }
                            else
                            {
                                specificTrigger.Position = Vector3.zero;
                                return false;
                            }
                        }
                        else if (landmarksData == null && !useBoundingBox)
                        {
                            specificTrigger.Position = Vector3.zero;
                            return false;
                        }
                        else
                        {
                            int landmarkId = landmarksData.QueryPointIndex(LandmarkToTrack);

                            PXCMFaceData.LandmarkPoint point = null;

                            landmarksData.QueryPoint(landmarkId, out point);

                            // Translation
                            if (point != null)
                            {
                                Vector3 vec = new Vector3();
                                vec.x = -point.world.x * 100f;
                                vec.y = point.world.y * 100f;
                                vec.z = point.world.z * 100f;

                                if (vec.x + vec.y + vec.z == 0)
                                {
                                    specificTrigger.Position = Vector3.zero;
                                    return false;
                                }

                                // Clamp and normalize to the Real World Box
                                TrackingUtilityClass.ClampToRealWorldInputBox(ref vec, RealWorldBoxCenter, RealWorldBoxDimensions);
                                TrackingUtilityClass.Normalize(ref vec, RealWorldBoxCenter, RealWorldBoxDimensions);

                                if (!float.IsNaN(vec.x) && !float.IsNaN(vec.y) && !float.IsNaN(vec.z))
                                {
                                    specificTrigger.Position = vec;
                                    success = true;
                                }
                            }

                            //Rotation
                            PXCMFaceData.PoseData poseData = singleFaceOutput.QueryPose();
                            if (success && poseData != null)
                            {
                                PXCMFaceData.PoseEulerAngles angles;
                                if (poseData.QueryPoseAngles(out angles))
                                {
                                    if (!float.IsNaN(angles.pitch) && !float.IsNaN(angles.yaw) && !float.IsNaN(angles.roll))
                                    {
                                        Quaternion q = Quaternion.Euler(-angles.pitch, angles.yaw, -angles.roll);

                                        specificTrigger.RotationQuaternion = q;

                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
