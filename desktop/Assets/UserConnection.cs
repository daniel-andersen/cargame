using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using UnityEngine;

public delegate void LineReceive(UserConnection sender, string Data);
public delegate void ConnectionClose(UserConnection sender);

public class UserConnection
{
	const int READ_BUFFER_SIZE = 2048;

	private TcpClient client;
	private byte[] readBuffer = new byte[READ_BUFFER_SIZE];
	private string strName;

	public UserConnection(TcpClient client)
	{
		this.client = client;
		this.client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
	}
	
	public string Name
	{
		get {
			return strName;
		}
		set {
			strName = value;
		}
	}
	
	public event LineReceive LineReceived;
	public event ConnectionClose ConnectionClosed;

	public void SendData(string Data)
	{
		lock (client.GetStream())
		{
			StreamWriter writer = new StreamWriter(client.GetStream());
			writer.Write(Data + (char) 13 + (char) 10);
			writer.Flush();
		}
	}
	
	private void StreamReceiver(IAsyncResult ar)
	{
		int BytesRead;
		string strMessage;

		try 
		{
			lock (client.GetStream())
			{
				BytesRead = client.GetStream().EndRead(ar);
			}
			if (BytesRead == 0)
			{
				Debug.Log ("No more data from client");
				disconnect();
				return;
			}
			strMessage = Encoding.ASCII.GetString(readBuffer, 0, Mathf.Min (BytesRead - 1, READ_BUFFER_SIZE));
			LineReceived(this, strMessage);

			lock (client.GetStream())
			{
				client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
			}
		} 
		catch (Exception e) {
			Debug.Log("Exception while receiving data: " + e.Message);
			disconnect();
		}
	}

	public void disconnect()
	{
		if (client != null) {
			client.GetStream ().Close ();
			client.Close ();
			client = null;
		}
		ConnectionClosed(this);
	}
}
