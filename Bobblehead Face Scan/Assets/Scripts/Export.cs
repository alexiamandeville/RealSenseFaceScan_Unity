using UnityEngine;
using System.Collections;

public class Export : MonoBehaviour {

	ObjExporterScript objExport;

	public Transform[] myMesh;


	void Start(){
		objExport = gameObject.GetComponent<ObjExporterScript> ();
	}

	public void MeshExport(){
		
		objExport.SaveModels (myMesh);

	}

}
