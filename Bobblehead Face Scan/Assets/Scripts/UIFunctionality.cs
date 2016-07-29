using UnityEngine;
using System.Collections;

public class UIFunctionality : MonoBehaviour {

	public Export myExporter;

	public void Export () {
		myExporter.MeshExport ();
	}
	

}
