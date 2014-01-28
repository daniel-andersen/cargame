using UnityEngine;
using System.Collections;

using ATrackClientConnection;

public class ATrackObject : MonoBehaviour {

	public static int objectCount = 10;
	public int objectId;
	public bool recognized;

	// Use this for initialization
	void Start () {
		transform.position = new Vector3 (-1000.0f, transform.position.y, -1000.0f);
		renderer.enabled = false;
		Debug.Log (objectId);
	}
	
	// Update is called once per frame
	void Update () {
		BlobRoot rootBlob = ATrackClientScript.newestBlob ();
		if (rootBlob == null || rootBlob.blobs.Count <= objectId) {
			transform.position = new Vector3 (-1000.0f, transform.position.y, -1000.0f);
			recognized = false;
			renderer.enabled = false;
		} else {
			transform.position = new Vector3((rootBlob.blobs[objectId].x * Util.screenScaleX * 2.0f) - Util.screenScaleX,
			                                 transform.position.y,
			                                 -((rootBlob.blobs[objectId].y * Util.screenScaleY * 2.0f) - Util.screenScaleY));
			renderer.enabled = true;
			recognized = true;
		}
	}
}
