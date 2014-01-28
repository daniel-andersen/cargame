using UnityEngine;
using System.Collections;

public class Flag : MonoBehaviour {

	public static int flagOwnershipCountMin = 100;
	public static int flagOwnershipCount = 0;
	public static CarMovement flagOwner = null;

	// Use this for initialization
	void Start () {
		randomizePosition ();
	}
	
	// Update is called once per frame
	void Update () {
		if (flagOwner != null) {
			flagOwnershipCount++;
		}
		updatePosition ();
	}

	private static void updatePosition()
	{
		if (flagOwner != null) {
			GameObject flagObject = GameObject.Find ("Flag");
			flagObject.transform.position = new Vector3(flagOwner.transform.position.x, flagObject.transform.position.y, flagOwner.transform.position.z);
		}
	}

	public static void updateOwnership(CarMovement car)
	{
		flagOwnershipCount = 0;
		flagOwner = car;
		updatePosition ();
		updateBaseAlpha ();
	}

	public static void bounceFlag()
	{
		if (flagOwner != null) {
			flagOwner = null;
			randomizePosition();
			updateBaseAlpha ();
		}
	}

	private static void updateBaseAlpha()
	{
		GameObject baseObject = GameObject.Find ("Base");
		Base baseScript = (Base)baseObject.GetComponent(typeof(Base));
		if (flagOwner != null) {
			baseScript.Show();
		} else {
			baseScript.Hide ();
		}
	}

	public static void randomizePosition()
	{
		GameObject flagObject = GameObject.Find ("Flag");

		for (int i = 0; i < 10; i++) {
			float x = Random.Range (-Util.screenScaleX * 0.6f, Util.screenScaleX * 0.6f);
			float z = Random.Range (-Util.screenScaleY * 0.6f, Util.screenScaleY * 0.6f);

			flagObject.transform.position = new Vector3 (x, flagObject.transform.position.y, z);

			bool obstacleNearby = false;

			for (int j = 0; j < ATrackObject.objectCount; j++) {
				GameObject obstacle = GameObject.Find ("ATrack Object " + (j + 1));
				
				Vector3 delta = flagObject.transform.position - obstacle.transform.position;
				if (delta.magnitude < obstacle.transform.localScale.magnitude) {
					obstacleNearby = true;
				}
			}

			if (!obstacleNearby) {
				return;
			}
		}
	}
}
