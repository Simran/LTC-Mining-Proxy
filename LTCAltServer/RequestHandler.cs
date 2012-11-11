using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace LTCAltServer
{
	public class RequestHandler
	{
		//Look around :D
		
		//Server Socket
		private Socket serverSocket = null;
		private Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		
		string serverRecv = null;
		string clientRecv = null;
		string worker = null;
		string pool = null;
		int port = 0;
	
		public RequestHandler(Socket serverSocket, string Pool, int PoolPort)
		{
			this.serverSocket = serverSocket;
			
			this.pool = Pool;
			int i = this.pool.IndexOf(':');
    		if (i > 0)
			{
				this.pool = this.pool.Substring(i + 1);
			}
			//this.pool = newPool;
			
			this.port = PoolPort;
		}
	
		public void Handle()		
		{
			while (true)
			{
				serverRecv = recv(serverSocket);
				if (Connected)
				{
					log(serverSocket, serverRecv); //Client Response
					//Send Client Request to the real server
					Send(clientSocket, serverRecv);
					//Receive Response from the real server through client socket
					clientRecv = recv(clientSocket);	
					log(clientSocket, clientRecv); //Server Response			
					//Send the real server response to the client through server socket
					Send(serverSocket, clientRecv);					
				}
				else
				{
					log(serverSocket, serverRecv); //Client Response		
					//Build C_Socket (client Socket)
					IPAddress[] addresslist = Dns.GetHostAddresses(pool);;
					IPEndPoint remoteEP = new IPEndPoint (addresslist[0], port);
					clientSocket.Connect (remoteEP);		
					//Send Client Request to the real server
					Send(clientSocket, serverRecv);		
					//Receive Response from the real server through client socket
					clientRecv = recv(clientSocket);				
					log(clientSocket, clientRecv); //Server Response				
					//Send the real server response to the client through server socket
					Send(serverSocket, clientRecv);
				}		
			}
		}
		//loop request
		
		public bool Connected
        {
            get
            {
				byte[] conBuf = new byte[1];
                try
                {
					clientSocket.Blocking = false;
                    clientSocket.Receive(conBuf, 1, SocketFlags.Peek);
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.WouldBlock)
                    {
						clientSocket.Blocking = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Functions.log(string.Format("Connected Exception: {0}", ex.Message), 3);
                }
				clientSocket.Blocking = true;
                return false;
            }
        }
		
		public string recv(Socket socket)
		{
			byte[] buffer = new byte[8192];
			int rec = socket.Receive(buffer);
            Array.Resize(ref buffer, rec);
			
			if (Encoding.Default.GetString(buffer).Contains ("Authorization"))
				{
				worker = Regex.Match(Encoding.Default.GetString(buffer), "Authorization: Basic (\\w+)").Groups[1].Value;
				}
			
			return Encoding.Default.GetString(buffer);
		}
	
	
		public void log(Socket socket, string data)
		{
			if (data.Contains ("getwork"))
			{
				Functions.log (string.Format ("Miner {0}@{1} is requesting work!", B64Decode(worker), ((IPEndPoint)socket.RemoteEndPoint).Address), 1);
				return;
			}
			if (data.Contains ("\"midstate\""))
			{
				Functions.log (string.Format ("Pool {0} has sent work to {1}!", pool, ((IPEndPoint)serverSocket.RemoteEndPoint).Address), 4);
				return;
			}
			if (data.Contains ("\"result\":true}"))
			{
				Functions.log (string.Format ("Pool {0}.. Accepted {1}.. YAY!!", pool, ((IPEndPoint)serverSocket.RemoteEndPoint).Address), 2);
				return;
			}	
			if (data.Contains ("\"result\":false}"))
			{
				Functions.log (string.Format ("Pool {0}.. Rejected {1}.. BOO!!", pool, ((IPEndPoint)serverSocket.RemoteEndPoint).Address), 3);
			}   
		}
		
		public int Send(Socket socket, string data)
		{
			return socket.Send(Encoding.Default.GetBytes(data));
		}
		
		public string B64Decode(string data)
        {
            byte[] decbuff = Convert.FromBase64String(data);
            return Encoding.Default.GetString(decbuff);
        }	
	}
}

