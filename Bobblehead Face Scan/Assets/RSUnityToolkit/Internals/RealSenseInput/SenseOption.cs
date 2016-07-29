using UnityEngine;
using System.Collections.Generic;

namespace RSUnityToolkit
{	

	public class SenseOption
	{
		#region Nested Types
		
		public enum SenseOptionID
        {
            None = 0,
            All = 0xFFFF,
            VideoColorStream = 0x0001,
            VideoDepthStream = 0x0002,
            Hand = 0x0004,
            Face = 0x0008,
            VideoIRStream = 0x0010,
            Blob = 0x0020,
			Speech = 0x0040,
			PointCloud = 0x0080,
			UVMap = 0x0100,
			Object = 0x0200,
			VideoSegmentation = 0x0400
        }
		
		#endregion
	
		#region Private Fields
		
		private SenseOptionID _id;
		
		#endregion
		
		#region Public Fields
		
		public int RefCounter;
		public int ModuleCUID;
		public bool Enabled = false;
		public bool Initialized = false;
		
		#endregion
		
		#region C'tor
		
		public SenseOption(SenseOptionID id)
		{
			_id = id;
			ModuleCUID = -1;
		}
			
		#endregion
		
		#region Public Properties
		
		public SenseOptionID ID
		{
			get
			{
				return _id;
			}
			private set
			{
				_id = value;
			}
		}
		
		#endregion
		
	}
}
