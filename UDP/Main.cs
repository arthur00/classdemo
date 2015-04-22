using System.Collections;
 
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

// This constructor arbitrarily assigns the local port number.
class MainClass
{
	class Player
    {
		[JsonProperty]
        internal string name;
		[JsonProperty]
        internal int[] coord;
		[JsonProperty]
		internal int velocity;
    }


	public static void Main(String[] args) {
		// Server
		byte[] data = new byte[1024];
		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
		UdpClient newsock = new UdpClient(ipep);

		Console.WriteLine("[SERVER]: Waiting for a client...");

		IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

		 // setup receiving thread
        Thread receiveThread = new Thread(delegate()
        {
			while(true)
			{
				data = newsock.Receive(ref sender);
				Console.WriteLine("\n[SERVER]: Message received from {0}:", sender.ToString());
				Console.WriteLine("[SERVER]: Received: " + Encoding.ASCII.GetString(data, 0, data.Length) + "\n");
				System.Threading.Thread.Sleep(1000);

				Player p = JsonConvert.DeserializeObject<Player>(Encoding.ASCII.GetString(data, 0, data.Length));
				Console.WriteLine ("[SERVER]: The player is {0}, in position {1},{2}, and has speed {3}",p.name,p.coord[0],p.coord[1],p.velocity);

				newsock.Send(data, data.Length, sender);
			}
	    });
        receiveThread.Start();


		// Client 
		UdpClient udpClient = new UdpClient(12000);
	    try{
			udpClient.Connect("localhost", 9050);

			// Sends a message to the host to which you have connected.
			Player p1 = new Player();
			p1.coord = new int[2];
			p1.coord[0] = 100;
			p1.coord[1] = 200;
			p1.name = "Arthur";
			p1.velocity = 100;
			string output = JsonConvert.SerializeObject(p1);
			Byte[] sendBytes = Encoding.ASCII.GetBytes(output);

			udpClient.Send(sendBytes, sendBytes.Length);

			// Sends a message to a different host using optional hostname and port parameters.
			UdpClient udpClientB = new UdpClient();
			udpClientB.Send(sendBytes, sendBytes.Length, "localhost", 9050);

			//IPEndPoint object will allow us to read datagrams sent from any source.
			IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

			// Blocks until a message returns on this socket from a remote host.
			Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint); 
			string returnData = Encoding.ASCII.GetString(receiveBytes);

			// Uses the IPEndPoint object to determine which of these two hosts responded.
			Console.WriteLine("[CLIENT]: Reply from SERVER: " +
			                          returnData.ToString() + "\n[CLIENT]: This message was sent from " +
			                         RemoteIpEndPoint.Address.ToString() +
			                         " on their port number " +
			                         RemoteIpEndPoint.Port.ToString());

			udpClient.Close();
			udpClientB.Close();

			}  
			catch (Exception e ) {
			  Console.WriteLine(e.ToString());
			}
	}
}