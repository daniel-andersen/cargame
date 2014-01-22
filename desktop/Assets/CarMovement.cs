using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarMovement : MonoBehaviour {

	// http://www.asawicki.info/Mirror/Car%20Physics%20for%20Games/Car%20Physics%20for%20Games.html

	public int carId = 0;
	public bool hasFlag = false;

	private const float COLLISION_DISTANCE = 5.0f;

	private const float OBSTACLE_LOOKAHEAD_DISTANCE = 20.0f;
	private const float OBSTACLE_BOUNCE_DAMPENING = 0.75f;

	private const float TARGET_LOOK_AHEAD_DISTANCE = 30.0f;

	private const float FLAG_CAPTURE_DISTANCE = 5.0f;

	private const float MAX_STEERING_ANGLE = Mathf.PI / 8.0f;

	private const float CA_R = -5.2f; // Cornering stiffness
	private const float CA_F = -5.0f;
	private const float MAX_GRIP = 7.0f;

	private const float dragConst = 5.0f;
	private const float rollingResistanceConst = 30.0f;
	
	private const float inertia = 200.0f;

	public float mass = 200.0f;

	//public Vector2 velocity = new Vector2(0.0f, 0.0f);

	public float angularVelocity = 0.0f;

	public float angle = 0.0f;
	public float steeringAngle = 0.0f;

	public float throttle = 0.0f;
	public float brake = 0.0f;

	public static HashSet<string> collisions = new HashSet<string>();

	/*void OnTriggerEnter(Collider other) {
		CarMovement otherCar = null;
		for (int i = 0; i < 4; i++) {
			if (other.gameObject == getCarObject(i)) {
				otherCar = getCarScript(i);
			}
		}
		if (otherCar == null) {
			return;
		}
		collisions.Add (carId + "" + otherCar.carId);
		collisions.Add (otherCar.carId + "" + carId);
	}

	void OnTriggerExit(Collider other) {
		CarMovement otherCar = null;
		for (int i = 0; i < 4; i++) {
			if (other.gameObject == getCarObject(i)) {
				otherCar = getCarScript(i);
			}
		}
		if (otherCar == null) {
			return;
		}
		collisions.Remove (carId + "" + otherCar.carId);
		collisions.Remove (otherCar.carId + "" + carId);
	}*/

	// Use this for initialization
	void Start () {
		if (name.Equals ("Player 1")) {
			carId = 0;
			mass = 200;
		}
		if (name.Equals ("Player 2")) {
			carId = 1;
			mass = 300;
		}
		if (name.Equals ("Player 3")) {
			carId = 2;
			mass = 250;
		}
		if (name.Equals ("Player 4")) {
			carId = 3;
			mass = 400;
		}
	}
	
	// Update is called once per frame
	void Update () {
		updateControls ();
		updateCar (Time.deltaTime);
		updateObstacleBounce ();
		//updateCarBounce ();
		clambCarToRoad ();
		updateFlagOwnership ();
		transform.position = new Vector3 (transform.position.x, Mathf.Max (0.0f, transform.position.y), transform.position.z);
	}

	private void clambCarToRoad()
	{
		transform.position = new Vector3(Mathf.Min (Util.screenScaleX, Mathf.Max (-Util.screenScaleX, transform.position.x)),
		                                 transform.position.y,
		                                 Mathf.Min (Util.screenScaleY, Mathf.Max (-Util.screenScaleY, transform.position.z)));
	}

	private void updateObstacleBounce()
	{
		// Walls
		/*if (isBeyondLeftBorder(planarVector(transform.position)) && rigidbody.velocity.x < 0.0f) {
			rigidbody.velocity = new Vector3(-rigidbody.velocity.x * OBSTACLE_BOUNCE_DAMPENING, rigidbody.velocity.y, rigidbody.velocity.z);
		}
		if (isBeyondRightBorder(planarVector(transform.position)) && rigidbody.velocity.x > 0.0f) {
			rigidbody.velocity = new Vector3(-rigidbody.velocity.x * OBSTACLE_BOUNCE_DAMPENING, rigidbody.velocity.y, rigidbody.velocity.z);
		}
		if (isBeyondTopBorder(planarVector(transform.position)) && rigidbody.velocity.z < 0.0f) {
			rigidbody.velocity = new Vector3(rigidbody.velocity.x, rigidbody.velocity.y, -rigidbody.velocity.z * OBSTACLE_BOUNCE_DAMPENING);
		}
		if (isBeyondBottomBorder(planarVector(transform.position)) && rigidbody.velocity.z > 0.0f) {
			rigidbody.velocity = new Vector3(rigidbody.velocity.x, rigidbody.velocity.y, -rigidbody.velocity.z * OBSTACLE_BOUNCE_DAMPENING);
		}*/
	}

	private void updateFlagOwnership()
	{
		if (Flag.flagOwner == this) {
			return;
		}

		GameObject flagObject = GameObject.Find ("Flag");
		
		float deltaX = transform.position.x - flagObject.transform.position.x;
		float deltaZ = transform.position.z - flagObject.transform.position.z;
		
		float len = Mathf.Sqrt (deltaX * deltaX + deltaZ * deltaZ);
		if (len == 0.0f) {
			return;
		}

		if (len < FLAG_CAPTURE_DISTANCE) {
			Flag.updateOwnership(this);
		}
	}

	private void updateControls ()
	{
		foreach (User user in Server.clients.Values) {
			if (user.carId == carId) {
				updatePhoneControls(user);
				return;
			}
		}
		if (name.Equals ("Player 2"))
		{
			updateKeyControls ();
		}
		else {
			updateComputerControlledCar ();
		}
	}

	private void updatePhoneControls(User user)
	{
		steeringAngle = Mathf.Max (-MAX_STEERING_ANGLE, Mathf.Min (MAX_STEERING_ANGLE, (float)user.steeringAngle));
		throttle = Mathf.Max (-100.0f, Mathf.Min (100.0f, (float)user.throttle));
		brake = Mathf.Max (0.0f, Mathf.Min (100.0f, (float)user.brake));
	}

	private void updateComputerControlledCar()
	{
		if (Flag.flagOwner == this) {
			updateComputerFlee();
		}
		else
		{
			updateComputerCatchFlag();
		}
	}

	private void updateComputerFlee()
	{
		Vector3 evade = new Vector3 (0.0f, 0.0f, 0.0f);

		for (int i = 0; i < 4; i++) {
			if (getCarObject(i) != this) {
				Vector3 delta = lookAhead(carId) - lookAhead(i);
				float distance = delta.magnitude;
				if (distance == 0.0f) {
					continue;
				}
				float distanceWeight = 4.0f / distance;
				evade += delta * distanceWeight;
			}
		}

		float destAngle = -Mathf.Atan2 (-evade.z, -evade.x) + (Mathf.PI + Mathf.PI / 2.0f);
		destAngle = clampAngle (destAngle);

		destAngle = accountForObstacles (destAngle, true);

		steerTowardsAngle (destAngle);
		throttle = 80.0f;
	}

	private void updateComputerCatchFlag()
	{
		Vector3 delta = new Vector3 ();
		if (Flag.flagOwner != null) {
			delta = transform.position - lookAhead(Flag.flagOwner.carId);
		} else {
			GameObject flagObject = GameObject.Find ("Flag");
			delta = transform.position - flagObject.transform.position;
		}

		float destAngle = -Mathf.Atan2 (delta.z, delta.x) + (Mathf.PI + Mathf.PI / 2.0f);
		destAngle = clampAngle (destAngle);

		steerTowardsAngle (destAngle);
		throttle = 100.0f;
	}

	private float clampAngle(float destAngle)
	{
		if (destAngle < 0.0f) {
			destAngle += Mathf.PI * 2.0f;
		}
		if (destAngle >= Mathf.PI * 2.0f) {
			destAngle -= Mathf.PI * 2.0f;
		}
		return destAngle;
	}

	private void steerTowardsAngle(float destAngle)
	{
		destAngle = clampAngle (destAngle);

		float closestAngle;
		if (Mathf.Abs (angle - (destAngle + (Mathf.PI * 2.0f))) < Mathf.PI) {
			closestAngle = destAngle + (Mathf.PI * 2.0f);
		} else if (Mathf.Abs (angle - (destAngle - (Mathf.PI * 2.0f))) < Mathf.PI) {
			closestAngle = destAngle - (Mathf.PI * 2.0f);
		} else {
			closestAngle = destAngle;
		}

		steeringAngle = Mathf.Min (Mathf.PI / 8.0f, Mathf.Max (-Mathf.PI / 8.0f, closestAngle - angle));
	}

	private float accountForObstacles(float destAngle, bool accountForBorders)
	{
		for (float deltaAngle = 0.0f; deltaAngle < Mathf.PI; deltaAngle += Mathf.PI / 32.0f) {
			float a1 = destAngle + deltaAngle;
			if (!hasObstacleAtDestAngle(a1, accountForBorders, deltaAngle == 0.0f)) {
				return a1;
			}
			float a2 = destAngle - deltaAngle;
			if (!hasObstacleAtDestAngle(a2, accountForBorders, false)) {
				return a2;
			}
		}
		return destAngle;
	}

	private bool hasObstacleAtDestAngle(float destAngle, bool accountForBorders, bool log)
	{
		float adjustedAngle = -destAngle + Mathf.PI / 2.0f;

		Vector2 destPosition = new Vector2 (transform.position.x + Mathf.Cos (adjustedAngle) * TARGET_LOOK_AHEAD_DISTANCE,
		                                    transform.position.z + Mathf.Sin (adjustedAngle) * TARGET_LOOK_AHEAD_DISTANCE);

		if (accountForBorders) {
			if (Mathf.Cos (adjustedAngle) > 0.0f && isBeyondRightBorder(destPosition))
			{
				return true;
			}
			if (Mathf.Cos (adjustedAngle) < 0.0f && isBeyondLeftBorder(destPosition))
			{
				return true;
			}
			if (Mathf.Sin (adjustedAngle) > 0.0f && isBeyondBottomBorder(destPosition))
			{
				return true;
			}
			if (Mathf.Sin (adjustedAngle) < 0.0f && isBeyondTopBorder(destPosition))
			{
				return true;
			}
		}

		return false;
	}

	private bool isBeyondLeftBorder(Vector2 position)
	{
		return position.x < -Util.screenScaleX;
	}

	private bool isBeyondRightBorder(Vector2 position)
	{
		return position.x > Util.screenScaleX;
	}

	private bool isBeyondTopBorder(Vector2 position)
	{
		return position.y < -Util.screenScaleY;
	}
	
	private bool isBeyondBottomBorder(Vector2 position)
	{
		return position.y > Util.screenScaleY;
	}

	private void updateKeyControls()
	{
		if (Input.GetKey (KeyCode.W))
		{
			throttle = Mathf.Min (100.0f, throttle + 10.0f);
			brake = 0.0f;
		}
		else if (Input.GetKey (KeyCode.S))
		{
			throttle = 0.0f;
			brake = 100.0f;
		}
		else {
			throttle = 0.0f;
			brake = Mathf.Min (50.0f, brake + 5.0f);
		}

		if (Input.GetKey (KeyCode.A))
		{
			steeringAngle = Mathf.Max (-Mathf.PI / 8.0f, steeringAngle - Mathf.PI / 64.0f);
		}
		else if (Input.GetKey (KeyCode.D))
		{
			steeringAngle = Mathf.Min ( Mathf.PI / 8.0f, steeringAngle + Mathf.PI / 64.0f);
		}
		else
		{
			steeringAngle = 0.0f;
		}
	}

	private void updateCar(float timeDelta)
	{
		// Velocity in local reference
		float sn = Mathf.Sin(angle);
		float cs = Mathf.Cos(angle);

		Vector2 localVelocity = new Vector2 ();
		localVelocity.x =  cs * rigidbody.velocity.z + sn * rigidbody.velocity.x;
		localVelocity.y = -sn * rigidbody.velocity.z + cs * rigidbody.velocity.x;

		// --- Lateral forces ---

		// Yaw speed
		float yawSpeed = 2.0f /*wheelbase*/ * 0.5f * angularVelocity;	

		// Rotation angle
		float rotationAngle = localVelocity.x == 0.0f ? 0.0f : Mathf.Atan2(yawSpeed, localVelocity.x);

		// Sideslip
		float sideslip = localVelocity.x == 0.0f ? 0.0f : Mathf.Atan2(localVelocity.y, localVelocity.x);

		// Slip angle front and rear
		float slipangleFront = sideslip + rotationAngle - steeringAngle;
		float slipangleRear  = sideslip - rotationAngle;

		// Weight per axle = half car mass times 1G (=9.8m/s^2) 
		float weight = mass * 9.8f * 0.5f;

		// Lateral force on front wheels
		Vector2 latFront = new Vector2 ();
		latFront.x = 0.0f;
		latFront.y = CA_F * slipangleFront;
		latFront.y = Mathf.Min( MAX_GRIP, latFront.y);
		latFront.y = Mathf.Max(-MAX_GRIP, latFront.y);
		latFront.y *= weight;

		// Lateral force on rear wheels
		Vector2 latRear = new Vector2 ();
		latRear.x = 0.0f;
		latRear.y = CA_R * slipangleRear;
		latRear.y = Mathf.Min( MAX_GRIP, latRear.y);
		latRear.y = Mathf.Max(-MAX_GRIP, latRear.y);
		latRear.y *= weight;

		// Torque from lateral forces
		float torque = latFront.y - latRear.y;

		// --- Longitudinal forces  ---

		// Traction
		Vector2 traction = new Vector2 ();
		traction.x = 100.0f * (throttle - (brake * Mathf.Sign(localVelocity.x)));
		traction.y = 0;

		// Rolling and air resistance
		Vector2 resistance = new Vector2 ();
		resistance.x = -(rollingResistanceConst * localVelocity.x + dragConst * localVelocity.x * Mathf.Abs(localVelocity.x));
		resistance.y = -(rollingResistanceConst * localVelocity.y + dragConst * localVelocity.y * Mathf.Abs(localVelocity.y));

		// Longitudinal force
		Vector2 longForce = new Vector2 ();

		longForce.x = traction.x + Mathf.Sin (steeringAngle) * latFront.x + latRear.x + resistance.x;
		longForce.y = traction.y + Mathf.Cos (steeringAngle) * latFront.y + latRear.y + resistance.y;

		// --- Accelleration ---

		// Longitudinal accelleration
		Vector2 accel = longForce / mass;

		// Angular acceleration
		float angularAcceleration = torque / inertia;

		// --- Velocity and position ---

		// transform acceleration from car reference frame to world reference frame
		Vector3 accelWorldCoord = new Vector3 ();
		accelWorldCoord.x =  cs * accel.y + sn * accel.x;
		accelWorldCoord.y = 0.0f;
		accelWorldCoord.z = -sn * accel.y + cs * accel.x;

		// Integrated velocity
		rigidbody.velocity += accelWorldCoord * timeDelta;

		// Integrated position
		transform.position += rigidbody.velocity * timeDelta;

		// --- Angular velocity and heading ---
		
		// Integrated angular velocity
		angularVelocity += angularAcceleration * timeDelta;

		// Integrated angle
		angle += angularVelocity * timeDelta;
		if (angle < 0.0f) {
			angle += Mathf.PI * 2.0f;
		}
		if (angle >= Mathf.PI * 2.0f) {
			angle -= Mathf.PI * 2.0f;
		}

		// --- Car visual ---
		
		// Body rotation
		Transform bodyTransform = transform.Find("Car");
		bodyTransform.localEulerAngles = new Vector3 (0.0f, angle * 180.0f / Mathf.PI, 0.0f);

		// Front left wheel rotation
		Transform frontLeftWheelTransform = bodyTransform.Find("Front Wheel Left");
		frontLeftWheelTransform.localEulerAngles = new Vector3 (0.0f, steeringAngle * 180.0f / Mathf.PI, 0.0f);

		// Front right wheel rotation
		Transform frontRightWheelTransform = bodyTransform.Find("Front Wheel Right");
		frontRightWheelTransform.localEulerAngles = new Vector3 (0.0f, steeringAngle * 180.0f / Mathf.PI, 0.0f);
	}

	private Vector3 lookAhead(int index) {
		GameObject carObject = getCarObject (index);
		Vector3 v = carObject.rigidbody.velocity.normalized * TARGET_LOOK_AHEAD_DISTANCE;
		return carObject.transform.position + v;
	}

	private Vector2 planarVector(Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	private GameObject getCarObject(int index)
	{
		return GameObject.Find ("Player " + (index + 1));
	}

	private CarMovement getCarScript(int index)
	{
		GameObject carObject = getCarObject (index);
		return (CarMovement)carObject.GetComponent(typeof(CarMovement));
	}
}
