using UnityEngine;
using System.Collections;

public class Intro : MonoBehaviour {

	private const float CAR_DISTANCE_IN = 150.0f;
	private const float CAR_DISTANCE_OUT = 1000.0f;

	private const float MOVE_SPEED_FORWARDS = 5.0f;
	private const float MOVE_VELOCITY_BACKWARDS = 130.0f;

	private Vector3 destinationIn;
	private Vector3 destinationOut;

	private int countdown = 0;

	// Use this for initialization
	void Start () {
		if (name.Equals ("Car 1")) {
			destinationIn  = new Vector3(-CAR_DISTANCE_IN,  0.0f,  CAR_DISTANCE_IN );
			destinationOut = new Vector3(-CAR_DISTANCE_OUT, 0.0f,  CAR_DISTANCE_OUT);
		}
		if (name.Equals ("Car 2")) {
			destinationIn  = new Vector3( CAR_DISTANCE_IN,  0.0f, -CAR_DISTANCE_IN );
			destinationOut = new Vector3( CAR_DISTANCE_OUT, 0.0f, -CAR_DISTANCE_OUT);
		}
		if (name.Equals ("Car 3")) {
			destinationIn  = new Vector3( CAR_DISTANCE_IN,  0.0f,  CAR_DISTANCE_IN );
			destinationOut = new Vector3( CAR_DISTANCE_OUT, 0.0f,  CAR_DISTANCE_OUT);
		}
		if (name.Equals ("Car 4")) {
			destinationIn  = new Vector3(-CAR_DISTANCE_IN,  0.0f, -CAR_DISTANCE_IN );
			destinationOut = new Vector3(-CAR_DISTANCE_OUT, 0.0f, -CAR_DISTANCE_OUT);
		}
	}
	
	// Update is called once per frame
	void Update () {
		countdown++;

		if (countdown < 600) {
			Vector3 delta = destinationIn - transform.position;
			delta *= 0.02f;
			if (delta.magnitude > MOVE_SPEED_FORWARDS) {
				delta = delta * MOVE_SPEED_FORWARDS / delta.magnitude;
			}
			transform.position += delta;
		} else if (countdown < 900) {
			Vector3 delta = destinationOut - transform.position;
			rigidbody.velocity += delta.normalized * 1.5f;
			if (rigidbody.velocity.magnitude > MOVE_VELOCITY_BACKWARDS) {
				rigidbody.velocity = rigidbody.velocity * MOVE_VELOCITY_BACKWARDS / rigidbody.velocity.magnitude;
			}
		} else {
			Application.LoadLevel("Level1");
		}
	}
}
