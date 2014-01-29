using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;

public class User {
	public string userId;
	public int carId;
	public UserConnection userConnection;

	public float steeringAngle;
	public float throttle;
	public float brake;
}

public class Server : MonoBehaviour {

	private const int CAR_COUNT = 4;
	private const int PORT_NUM = 20021;

	public static Dictionary<string, User> clients = new Dictionary<string, User>();

	private TcpListener tcpListener;
	private Thread listenerThread;

	// Use this for initialization
	void Start () {
		Debug.Log("Starting connection listener thread...");
		listenerThread = new Thread(new ThreadStart(DoListen));
		listenerThread.Start();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnApplicationQuit()
	{
		Disconnect ();
	}

	private void Disconnect()
	{
		if (tcpListener != null) {
			Debug.Log ("Stopping connection...");
			tcpListener.Stop ();
			tcpListener = null;
		}
	}

	public static User getUserWithCarId(int carId)
	{
		foreach (User user in clients.Values) {
			if (user.carId == carId) {
				return user;
			}
		}
		return null;
	}

	private void ConnectUser(string userName, UserConnection sender)
	{
		Debug.Log ("Connecting user: " + userName);
		if (clients.ContainsKey(userName) || clients.Count >= CAR_COUNT)
		{
			Debug.Log ("User already exists or too many cars (" + clients.Count + " / " + CAR_COUNT + ")");
			ReplyToSender("REFUSE", sender);
		}
		else 
		{
			sender.Name = userName;
			User user = new User();
			user.userId = userName;
			user.userConnection = sender;
			user.carId = GetVacantCarId();
			clients.Add(userName, user);
			CarMovement.hasPlayerController = true;
			ReplyToSender("JOIN|" + user.carId, sender);
			Debug.Log ("Car " + user.carId + " joined!");
		}
	}

	private void DisconnectUser(UserConnection sender)
	{
		clients.Remove(sender.Name);
	}

	private void Broadcast(string strMessage)
	{
		foreach (User user in clients.Values)
		{
			user.userConnection.SendData(strMessage);
		}
	}

	public void sendMessageToUser(string strMessage, User user)
	{
		user.userConnection.SendData(strMessage);
	}

	private void ReplyToSender(string strMessage, UserConnection sender)
	{
		sender.SendData(strMessage);
	}

	private void DoListen()
	{
		try {
			tcpListener = new TcpListener(System.Net.IPAddress.Any, PORT_NUM);
			tcpListener.Start();
			
			Debug.Log ("Server listening on port " + PORT_NUM + "...");

			while (true)
			{
				UserConnection client = new UserConnection(tcpListener.AcceptTcpClient());
				client.LineReceived += new LineReceive(OnLineReceived);
				client.ConnectionClosed += new ConnectionClose(OnConnectionClosed);
				Debug.Log ("New client connection!");
			}
		} 
		catch (Exception e) {
			Debug.Log ("Exception while listening for connections: " + e.Message);
		}
	}

	private void OnConnectionClosed(UserConnection sender)
	{
		Debug.Log ("User " + sender.Name + " disconnected!");
		DisconnectUser (sender);
	}

	private void OnLineReceived(UserConnection sender, string data)
	{
		string[] dataArray = data.Split((char) 13);

		dataArray = dataArray[0].Split((char) 124);

		switch( dataArray[0])
		{
		case "CONNECT":
			ConnectUser(dataArray[1], sender);
			break;
		case "DISCONNECT":
			DisconnectUser(sender);
			break;
		case "UPDATE":
			HandleUpdate(sender, dataArray);
			break;
		default: 
			break;
		}
	}

	private void HandleUpdate(UserConnection sender, string[] dataArray)
	{
		clients [sender.Name].steeringAngle = (float)Convert.ToDouble (dataArray [1]);
		clients [sender.Name].throttle = (float)Convert.ToDouble (dataArray [2]);
		clients [sender.Name].brake = (float)Convert.ToDouble (dataArray [3]);
		ReplyToSender ("READY", sender);
	}

	private int GetVacantCarId()
	{
		bool[] carIdUsed = new bool[CAR_COUNT];
		for (int i = 0; i < CAR_COUNT; i++) {
			carIdUsed[i] = false;
		}
		foreach (User user in clients.Values) {
			carIdUsed[user.carId] = true;
		}
		for (int i = 0; i < CAR_COUNT; i++) {
			if (!carIdUsed[i]) {
				return i;
			}
		}
		return 0;
	}
}
