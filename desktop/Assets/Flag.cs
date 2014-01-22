using UnityEngine;
using System.Collections;

public class Flag : MonoBehaviour {

	public static int OWNERSHIP_COUNTDOWN = 50;

	public static CarMovement flagOwner = null;
	public static int flagOwnershipCountdown = 0;

	// Use this for initialization
	void Start () {
		randomizePosition ();
	}
	
	// Update is called once per frame
	void Update () {
		updatePosition ();
		flagOwnershipCountdown = Mathf.Max (0, flagOwnershipCountdown - 1);
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
		if (flagOwnershipCountdown == 0) {
			flagOwner = car;
			updatePosition ();
			flagOwnershipCountdown = OWNERSHIP_COUNTDOWN;
		}
	}

	public void randomizePosition()
	{
		float x = Random.Range (-Util.screenScaleX, Util.screenScaleX);
		float z = Random.Range (-Util.screenScaleY, Util.screenScaleY);
		transform.position = new Vector3 (x, transform.position.y, z);
	}
}
