using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

// State object for receiving data from remote device.
public class StateObject {
    // Client socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 256;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
    // ManualResetEvent instances signal completion.
    public AutoResetEvent connectDone =
        new AutoResetEvent(false);

    public AutoResetEvent receiveDone =
        new AutoResetEvent(false);

    public AutoResetEvent sendDone =
        new AutoResetEvent(false);

    // The response from the remote device.
    public String response = String.Empty;
}

public class AsynchronousClient {
    // The port number for the remote device.
    private const int port = 11000;
    static string[] stringSeparators = new string[] { "<EOF>" };
    

    private void StartClient() {
        // Connect to a remote device.
        try {
            // Establish the remote endpoint for the socket.
            // The name of the 
            // remote device is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            StateObject send_so = new StateObject();
            send_so.workSocket = client;
            // Connect to the remote endpoint.
            client.BeginConnect( remoteEP, 
                new AsyncCallback(ConnectCallback), send_so);

            // Waits for 5 seconds for connection to be done
            send_so.connectDone.WaitOne(5000);

            // Send test data to the remote device.
            Send(client,"This is a test<EOF>", send_so);
            send_so.sendDone.WaitOne(5000);

			// Send test data to the remote device.
            Send(client,"Test 2<EOF>", send_so);
            send_so.sendDone.WaitOne(5000);

			// Send test data to the remote device.
            Send(client,"Test 3<EOF>", send_so);
            send_so.sendDone.WaitOne(5000);

            
            // Receive the response from the remote device.
            // Create the state object for receiving.
            StateObject recv_so = new StateObject();
            recv_so.workSocket = client;

            Receive(recv_so);
            recv_so.receiveDone.WaitOne(5000);

            // Write the response to the console.
            Console.WriteLine("Response received : {0}", recv_so.response);
            
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void ConnectCallback(IAsyncResult ar) {
        try {
            // Create the state object.
            StateObject state = (StateObject)ar.AsyncState;
            // Retrieve the socket from the state object.
            Socket client = state.workSocket;

            // Complete the connection.
            client.EndConnect(ar);

            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            state.connectDone.Set();
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private void Receive(StateObject state) {
        try
        {
            Socket client = state.workSocket;
            
            // Begin receiving the data from the remote device.
            client.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void ReceiveCallback( IAsyncResult ar ) {
        try {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject) ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0) {
                // Found a 
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                string content = state.sb.ToString();
                
                String[] message = content.Split(stringSeparators, StringSplitOptions.None);
                if (message.Length == 2)
                {
                    state.receiveDone.Set();
                    state.response = message[0];

                    state.workSocket.Shutdown(SocketShutdown.Both);
                    state.workSocket.Close();
                    
                }
                else
                {
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
            } else {
                Console.WriteLine("Connection close has been requested.");
                // Signal that all bytes have been received.
                
            }
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private void Send(Socket client, String data, StateObject so) {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), so);
    }

    private static void SendCallback(IAsyncResult ar) {
        try {
            // Retrieve the socket from the state object.
            StateObject so = (StateObject) ar.AsyncState;
            Socket client = so.workSocket;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.
            so.sendDone.Set();
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }
    
    public static int Main(String[] args) {
        AsynchronousClient client = new AsynchronousClient();
        Thread.Sleep(3000);
        client.StartClient();
        return 0;
    }
}
