using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using ATrackClientConnection;

public class ATrackObject : MonoBehaviour {

	public static int objectCount = 10;
	public static HashSet<int> blobIds = new HashSet<int> ();

	public int objectId;
	public bool recognized;

	private int blobId;

	// Use this for initialization
	void Start () {
		transform.position = new Vector3 (-1000.0f, transform.position.y, -1000.0f);
		renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		BlobRoot rootBlob = ATrackClientScript.newestBlob ();

		bool foundExistingBlob = rootBlob != null && blobId != -1 && rootBlob.blobs.ContainsKey (blobId);
		bool foundNewBlob = rootBlob != null && !foundExistingBlob && !blobIds.SetEquals (rootBlob.blobs.Keys);

		if (blobId != -1 && !foundExistingBlob && blobIds.Contains(blobId)) {
			blobIds.Remove (blobId);
		}

		if (!foundExistingBlob && !foundNewBlob) {
			recognized = false;
			blobId = -1;
			transform.position = new Vector3 (-1000.0f, transform.position.y, -1000.0f);
			renderer.enabled = false;
			return;
		}

		if (foundNewBlob) {
			foreach (int otherBlobId in rootBlob.blobs.Keys) {
				if (!blobIds.Contains(otherBlobId)) {
					blobId = otherBlobId;
					blobIds.Add (blobId);
					break;
				}
			}
		}

		Vector3 destPosition = new Vector3((rootBlob.blobs[blobId].x * Util.screenScaleX * 2.0f) - Util.screenScaleX,
		                                   transform.position.y,
		                                   -((rootBlob.blobs[blobId].y * Util.screenScaleY * 2.0f) - Util.screenScaleY));

		if (foundNewBlob) {
			transform.position = destPosition;
		}

		rigidbody.MovePosition (transform.position + ((destPosition - transform.position) * 0.1f));

		renderer.enabled = false;
		recognized = true;
	}
}
