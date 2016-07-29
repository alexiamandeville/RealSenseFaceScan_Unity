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
    /// Hand tracking rule - Processes Track Triggers
    /// </summary>
    [AddComponentMenu("")]
	[TrackTrigger.TrackTriggerAtt]
	public class BlobTrackingRule : BaseRule
    {

        #region Public Fields

        /// <summary>
        /// The real world box center. In centimiters.
        /// </summary>
        public Vector3 RealWorldBoxCenter = new Vector3(0, 0, 50);

        /// <summary>
        /// The real world box dimensions. In Centimiters.
        /// </summary>
        public Vector3 RealWorldBoxDimensions = new Vector3(100, 100, 100);
		
		public float MaxDistance = 50;

        public PXCMBlobData.ExtremityType BlobPointToTrack = PXCMBlobData.ExtremityType.EXTREMITY_CLOSEST;
		
		public int BlobIndex = 0;
		
        #endregion

        #region Private Fields
		
		private float[] _depthArray;
		private PXCMPoint3DF32[] _pos_uvz;
     	private PXCMPoint3DF32[] _pos3d;
		
		#endregion

        #region C'tors

		public BlobTrackingRule(): base()
        {
            FriendlyName = "Blob Tracking";
        }

        #endregion

        #region Public Methods


        override public string GetIconPath()
        {
            return @"RulesIcons/object-tracking";
        }

        override public string GetRuleDescription()
        {
            return "Track blob's position";
        }

        protected override bool OnRuleEnabled()
        {
			SenseToolkitManager.Instance.SetSenseOption(SenseOption.SenseOptionID.Blob);
            return true;
        }
			
		protected override void OnRuleDisabled()
		{
			SenseToolkitManager.Instance.UnsetSenseOption(SenseOption.SenseOptionID.Blob);
		}
		
        public override bool Process(Trigger trigger)
        {
            trigger.ErrorDetected = false;

            if (SenseToolkitManager.Instance != null)
            {
                if (!SenseToolkitManager.Instance.IsSenseOptionSet(SenseOption.SenseOptionID.Blob))
                {
                    trigger.ErrorDetected = true;
                    Debug.LogError("Blob Module Not Set");
                    return false;
                }
            }

            if (!(trigger is TrackTrigger))
            {
                trigger.ErrorDetected = true;
                return false;
            }

            bool success = false;

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

            if (SenseToolkitManager.Instance != null && SenseToolkitManager.Instance.Initialized && SenseToolkitManager.Instance.BlobDataOutput != null)
            {
                int numberOfBlobsDetected = SenseToolkitManager.Instance.BlobDataOutput.QueryNumberOfBlobs();
                PXCMBlobData.IBlob iblob;

                // GZ: do we need to set this one more time ??? 
				// Setting max distance for this rule and process the image
                // SenseToolkitManager.Instance.BlobExtractor.SetMaxDistance(MaxDistance * 10);

                if (numberOfBlobsDetected > 0)									
                {
                    if (BlobIndex >= numberOfBlobsDetected)
					{
						return false;
					}

                    // Process Tracking 
                    SenseToolkitManager.Instance.BlobDataOutput.QueryBlobByAccessOrder(BlobIndex, PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out iblob);

                    if (iblob != null)
                    {
                        TrackTrigger specificTrigger = (TrackTrigger)trigger;

                        PXCMPoint3DF32 trackedPoint = iblob.QueryExtremityPoint(BlobPointToTrack);



                        Vector3 position = new Vector3();
                        position.x = -trackedPoint.x / 10;
                        position.y = trackedPoint.y / 10;
                        position.z = trackedPoint.z / 10;

                        TrackingUtilityClass.ClampToRealWorldInputBox(ref position, RealWorldBoxCenter, RealWorldBoxDimensions);
                        TrackingUtilityClass.Normalize(ref position, RealWorldBoxCenter, RealWorldBoxDimensions);

                        if (!float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z))
                        {
                            specificTrigger.Position = position;
                        }
                        else
                        {
                            return false;
                        }

                        success = true;
                    }
				}				
            }
            else
            {
                return false;
            }

            return success;
        }

        #endregion
    }
}