using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarMovement : MonoBehaviour {

	// http://www.asawicki.info/Mirror/Car%20Physics%20for%20Games/Car%20Physics%20for%20Games.html

	public int carId = 0;

	public static bool hasPlayerController = false;

	private const float FLAG_BURDEN = 0.05f;
	private const float COMPUTER_BURDEN = 0.1f;

	private const float COLLISION_DISTANCE = 5.0f;

	private const float OBSTACLE_LOOKAHEAD_DISTANCE = 20.0f;
	private const float OBSTACLE_BOUNCE_DAMPENING = 0.75f;

	private const float TARGET_LOOK_AHEAD_DISTANCE = 30.0f;
	private const float FLEE_LOOK_AHEAD_DISTANCE = 10.0f;

	private const float FLAG_CAPTURE_DISTANCE = 5.0f;

	private const float MAX_STEERING_ANGLE = Mathf.PI / 8.0f;
	private const float MAX_STEERING_ANGLE_CHANGE = Mathf.PI / 64.0f;

	private float CA_R = -5.2f; // Cornering stiffness
	private float CA_F = -5.0f;
	private float MAX_GRIP = 7.0f;

	private float dragConst = 5.0f;
	private float rollingResistanceConst = 30.0f;
	
	private const float inertia = 200.0f;

	public float angularVelocity = 0.0f;

	public float angle = 0.0f;
	public float steeringAngle = 0.0f;

	public float throttle = 0.0f;
	public float brake = 0.0f;

	public int score = 0;

	// Use this for initialization
	void Start () {
		if (name.Equals ("Player 1")) {
			carId = 0;
		}
		if (name.Equals ("Player 2")) {
			carId = 1;
		}
		if (name.Equals ("Player 3")) {
			carId = 2;
		}
		if (name.Equals ("Player 4")) {
			carId = 3;
		}
	}
	
	// Update is called once per frame
	void Update () {
		updateControls ();
		updateCar (Time.deltaTime);
		updateObstacleBounce ();
		clambCarToRoad ();
		updateFlagOwnership ();
		updateReachBase ();
	}

	void OnTriggerEnter(Collider other) {
		if (Flag.flagOwner == this) {
			CarMovement otherCar = (CarMovement)other.gameObject.GetComponent(typeof(CarMovement));
			if (otherCar != null) {
				Flag.bounceFlag();
			}
		}
	}

	private void clambCarToRoad()
	{
		transform.position = new Vector3(Mathf.Min (Util.screenScaleX, Mathf.Max (-Util.screenScaleX, transform.position.x)),
		                                 Mathf.Max (0.0f, transform.position.y),
		                                 Mathf.Min (Util.screenScaleY, Mathf.Max (-Util.screenScaleY, transform.position.z)));
	}

	private void updateObstacleBounce()
	{
	}

	private void updateReachBase()
	{
		if (Flag.flagOwner == null || Flag.flagOwner != this) {
			return;
		}
		GameObject baseObject = GameObject.Find ("Base");
		Vector3 delta = transform.position - baseObject.transform.position;
		if (delta.magnitude < 10.0f) {
			Flag.randomizePosition();
			Flag.flagOwner = null;
			addScore();
			User user = Server.getUserWithCarId(carId);
			if (user != null) {
				GameObject serverObject = GameObject.Find ("Server");
				Server server = (Server)serverObject.GetComponent(typeof(Server));
				server.sendMessageToUser("REACHED_BASE_WITH_FLAG", user);
			}
		}
	}

	private void addScore()
	{
		score++;

		float offset = (score - 1) * 5.0f;
		float borderDist = 0.0f;
		float rotation = 0.0f;

		Vector3 position = new Vector3 (0.0f, 5.0f, 0.0f);
		if (carId == 0) {
			position = new Vector3 (-Util.screenScaleX + borderDist + offset, 5.0f, Util.screenScaleY - borderDist);
			rotation = 45.0f + (90.0f * 0.0f);
		}
		if (carId == 1) {
			position = new Vector3 (Util.screenScaleX - borderDist - offset, 5.0f, Util.screenScaleY - borderDist);
			rotation = 45.0f + (90.0f * 0.0f);
		}
		if (carId == 2) {
			position = new Vector3 (-Util.screenScaleX + borderDist + offset, 5.0f, -Util.screenScaleY + borderDist);
			rotation = 45.0f + (90.0f * 0.0f);
		}
		if (carId == 3) {
			position = new Vector3 (Util.screenScaleX - borderDist - offset, 5.0f, -Util.screenScaleY + borderDist);
			rotation = 45.0f + (90.0f * 0.0f);
		}

		GameObject flag = GameObject.Find ("Flag " + (carId + 1));
		GameObject clone = Instantiate (flag, position, Quaternion.identity) as GameObject;
		clone.renderer.enabled = true;
		clone.transform.position = position;
		clone.transform.localEulerAngles = new Vector3 (90.0f, rotation, 0.0f);
		clone.renderer.material.color = new Vector4 (clone.renderer.material.color.r, clone.renderer.material.color.g, clone.renderer.material.color.b, 0.3f);
	}

	private void updateFlagOwnership()
	{
		if (Flag.flagOwner != null) {
			return;
		}
		GameObject flagObject = GameObject.Find ("Flag");
		Vector2 delta = planarVector(transform.position - flagObject.transform.position);
		if (delta.magnitude < FLAG_CAPTURE_DISTANCE) {
			Flag.updateOwnership(this);
		}
	}

	private void updateControls ()
	{
		User user = Server.getUserWithCarId (carId);
		if (user != null) {
			updatePhoneControls(user);
		} /*else if (name.Equals ("Player 2"))
		{
			updateKeyControls ();
		}*/ else {
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
		Vector3 carLookAheadPosition = lookAhead (carId, TARGET_LOOK_AHEAD_DISTANCE);

		Vector3 evade = new Vector3 (0.0f, 0.0f, 0.0f);

		// Move towards base
		GameObject baseObject = GameObject.Find ("Base");
		//evade -= (transform.position - baseObject.transform.position).normalized * 1.7f;
		evade -= evadeVector(transform.position, baseObject.transform.position, 64.0f);

		// Flee from enemies
		for (int i = 0; i < 4; i++) {
			if (getCarObject(i) != this) {
				evade += evadeVector(carLookAheadPosition, lookAhead(i, FLEE_LOOK_AHEAD_DISTANCE), 8.0f);
			}
		}

		// Move away test
		/*evade += evadeVectorSquare(carLookAheadPosition, new Vector3( 20.0f, 0.0f,   0.0f), 50.0f);
		evade += evadeVectorSquare(carLookAheadPosition, new Vector3(-20.0f, 0.0f,   0.0f), 50.0f);
		evade += evadeVectorSquare(carLookAheadPosition, new Vector3(  0.0f, 0.0f,  20.0f), 50.0f);
		evade += evadeVectorSquare(carLookAheadPosition, new Vector3(  0.0f, 0.0f, -20.0f), 50.0f);*/

		// Move away from borders
		/*Vector3 topBorder = new Vector3 (carLookAheadPosition.x, carLookAheadPosition.y, Util.screenScaleY);
		Vector3 bottomBorder = new Vector3 (carLookAheadPosition.x, carLookAheadPosition.y, -Util.screenScaleY);
		Vector3 leftBorder = new Vector3 (-Util.screenScaleX, carLookAheadPosition.y, carLookAheadPosition.z);
		Vector3 rightBorder = new Vector3 (Util.screenScaleX, carLookAheadPosition.y, carLookAheadPosition.z);

		evade += evadeVectorSquare(carLookAheadPosition, topBorder, 250.0f);
		evade += evadeVectorSquare(carLookAheadPosition, bottomBorder, 250.0f);
		evade += evadeVectorSquare(carLookAheadPosition, leftBorder, 250.0f);
		evade += evadeVectorSquare(carLookAheadPosition, rightBorder, 250.0f);*/

		// Calculate angle
		float destAngle = -Mathf.Atan2 (-evade.z, -evade.x) + (Mathf.PI + Mathf.PI / 2.0f);
		destAngle = clampAngle (destAngle);

		destAngle = accountForObstacles (destAngle);

		steerTowardsAngle (destAngle);
		throttle = 80.0f;
	}

	private Vector3 evadeVector(Vector3 position, Vector3 targetPosition, float distanceWeight) {
		Vector3 delta = position - targetPosition;
		if (delta.magnitude == 0.0f) {
			return new Vector3(0.0f, 0.0f, 0.0f);
		} else {
			return delta.normalized * (distanceWeight / delta.magnitude);
		}
	}

	private Vector3 evadeVectorSquare(Vector3 position, Vector3 targetPosition, float distanceWeight) {
		Vector3 delta = position - targetPosition;
		if (delta.magnitude == 0.0f) {
			return new Vector3(0.0f, 0.0f, 0.0f);
		} else {
			return delta.normalized * (distanceWeight / (delta.magnitude * delta.magnitude));
		}
	}

	private void updateComputerCatchFlag()
	{
		Vector3 delta = new Vector3 ();
		if (Flag.flagOwner != null) {
			delta = transform.position - lookAhead(Flag.flagOwner.carId, TARGET_LOOK_AHEAD_DISTANCE);
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

		steeringAngle = Mathf.Min (MAX_STEERING_ANGLE, Mathf.Max (-MAX_STEERING_ANGLE, closestAngle - angle));
	}

	private float accountForObstacles(float destAngle)
	{
		for (float deltaAngle = 0.0f; deltaAngle < Mathf.PI; deltaAngle += Mathf.PI / 32.0f) {
			float a1 = destAngle + deltaAngle;
			if (!hasObstacleAtDestAngle(a1)) {
				return a1;
			}
			float a2 = destAngle - deltaAngle;
			if (!hasObstacleAtDestAngle(a2)) {
				return a2;
			}
		}
		return destAngle;
	}

	private bool hasObstacleAtDestAngle(float destAngle)
	{
		/*float adjustedAngle = -destAngle + Mathf.PI / 2.0f;

		Vector2 destPosition = new Vector2 (transform.position.x + Mathf.Cos (adjustedAngle) * TARGET_LOOK_AHEAD_DISTANCE,
		                                    transform.position.z + Mathf.Sin (adjustedAngle) * TARGET_LOOK_AHEAD_DISTANCE);*/

		/*if (accountForBorders) {
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
		}*/

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
			steeringAngle = Mathf.Max (-MAX_STEERING_ANGLE, steeringAngle - Mathf.PI / 64.0f);
		}
		else if (Input.GetKey (KeyCode.D))
		{
			steeringAngle = Mathf.Min ( MAX_STEERING_ANGLE, steeringAngle + Mathf.PI / 64.0f);
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
		float weight = rigidbody.mass * 9.8f * 0.5f;

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

		// Burden
		float burden = 0.0f;
		if (Flag.flagOwner == this) {
			burden = FLAG_BURDEN;
		}
		if (hasPlayerController && Server.getUserWithCarId(carId) == null) {
			burden = COMPUTER_BURDEN;
		}

		// Traction
		Vector2 traction = new Vector2 ();
		traction.x = 150.0f * (1.0f - burden) * (throttle - (brake * Mathf.Sign(localVelocity.x)));
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
		Vector2 accel = longForce / rigidbody.mass;

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
		transform.localEulerAngles = new Vector3 (0.0f, angle * 180.0f / Mathf.PI, 0.0f);

		Transform bodyTransform = transform.Find("Car");

		// Front left wheel rotation
		Transform frontLeftWheelTransform = bodyTransform.Find("Front Wheel Left");
		frontLeftWheelTransform.localEulerAngles = new Vector3 (0.0f, steeringAngle * 180.0f / Mathf.PI, 0.0f);

		// Front right wheel rotation
		Transform frontRightWheelTransform = bodyTransform.Find("Front Wheel Right");
		frontRightWheelTransform.localEulerAngles = new Vector3 (0.0f, steeringAngle * 180.0f / Mathf.PI, 0.0f);
	}

	private Vector3 lookAhead(int index, float distance) {
		GameObject carObject = getCarObject (index);
		Vector3 v = carObject.rigidbody.velocity.normalized * distance;
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
