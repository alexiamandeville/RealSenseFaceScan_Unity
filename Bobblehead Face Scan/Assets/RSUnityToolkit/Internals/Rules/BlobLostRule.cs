/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2012-2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RSUnityToolkit
{
    /// <summary>
    /// Blob lost rule - Process Event trigger
    /// </summary>	
    [AddComponentMenu("")]
	[EventTrigger.EventTriggerAtt]
    public class BlobLostRule : BaseRule
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

        public PXCMBlobData.ExtremityType BlobPointToTrack = PXCMBlobData.ExtremityType.EXTREMITY_CLOSEST;
		
       	public float MaxDistance = 50;
		
		public int NumberOfBlobs = 1;
		
        #endregion

        #region Private Fields

        /// <summary>
        /// Store if in the last frame a blob was detected.
        /// </summary>
        private bool _lastFrameDetected = false;
		
        #endregion

        #region C'tor

        public BlobLostRule(): base()
        {
            FriendlyName = "Blob Lost";
        }

        #endregion

        #region Public Methods

        override public string GetIconPath()
        {
            return @"RulesIcons/object-lost";
        }

        override public string GetRuleDescription()
        {
            return "Fires whenever a blob is previously detected and then lost";
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
            if (!SenseToolkitManager.Instance.IsSenseOptionSet(SenseOption.SenseOptionID.VideoDepthStream))
            {
                trigger.ErrorDetected = true;
                Debug.LogError("Blob Module Not Set");
                return false;
            }
             
            if (!(trigger is EventTrigger))
            {
                trigger.ErrorDetected = true;
                return false;
            }

            EventTrigger specificTrigger = (EventTrigger)trigger;
            specificTrigger.Source = this.name;

            bool success = false;

            if (SenseToolkitManager.Instance.Initialized && SenseToolkitManager.Instance.BlobDataOutput != null)
            {

                int numberOfBlobsDetected = SenseToolkitManager.Instance.BlobDataOutput.QueryNumberOfBlobs();
                if (numberOfBlobsDetected >= NumberOfBlobs)
                {					
					int blobsRightlyDetected = 0;
                    PXCMBlobData.IBlob iblob;
					
					// For each detected blob, check that it is in our real world box
					for (int i = 0; i < numberOfBlobsDetected; i++)
					{
                        SenseToolkitManager.Instance.BlobDataOutput.QueryBlobByAccessOrder(i, PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out iblob);

                        if (iblob != null)
                        {
                            // Process Tracking                    
                            PXCMPoint3DF32 trackedPoint = iblob.QueryExtremityPoint(BlobPointToTrack);

                            Vector3 position = new Vector3();
                            position.x = -trackedPoint.x / 10;
                            position.y = trackedPoint.y / 10;
                            position.z = trackedPoint.z / 10;

                            TrackingUtilityClass.ClampToRealWorldInputBox(ref position, RealWorldBoxCenter, RealWorldBoxDimensions);
                            TrackingUtilityClass.Normalize(ref position, RealWorldBoxCenter, RealWorldBoxDimensions);

                            if (!float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z))
                            {
                                if (position.x > 0 && position.x < 1 &&
                                    position.y > 0 && position.y < 1 &&
                                    position.z > 0 && position.z < 1)
                                {
                                    blobsRightlyDetected++;

                                    if (blobsRightlyDetected == NumberOfBlobs)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                _lastFrameDetected = false;
                                return false;
                            }
                        }
					}
					
					if (blobsRightlyDetected < NumberOfBlobs) 
                    {
						if (_lastFrameDetected)
						{
                        	success = true;
						}
						_lastFrameDetected = false;
                    }
					else 
					{
						_lastFrameDetected = true;
					} 
                }
                else
                {
                    if (_lastFrameDetected)
                    {
                        success = true;
                    }
                    _lastFrameDetected = false;
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