using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using IniParser;
using System.Collections.Generic;

namespace LTCAltServer
{
	class MainClass
	{
		
	const int BACKLOG = 100;		
		
		
		
	static void Main(string[] args)
        {
			Functions.InitColors();
			
			FileIniDataParser Info = new FileIniDataParser();
			IniData Values = null;
			try
			{
				Values = Info.LoadFile("config.ini");			
			}
			catch 
			{ 
				Console.WriteLine ("Could not read config.ini!"); 
				Console.ReadLine ();
				return;
			}
			
			IPAddress IP = IPAddress.Any;
			
			string[] serverInfo = new string[] 
			{ 
			  	Values["SERVER"]["SERVPORT"], 
			  	Values["POOL"]["POOL"], 
			  	Values["POOL"]["POOLPORT"], 
			};
			
			foreach(string s in serverInfo)
			{
				if (string.IsNullOrEmpty (s))
				{
					Console.WriteLine("Error in config.ini!");
					Console.ReadLine ();
					return;
				}
			}
			
			startServer (IP, int.Parse(serverInfo[0]), serverInfo[1], int.Parse(serverInfo[2]));
        }
		
	static void startServer(IPAddress IP, int Port, string Pool, int PoolPort)
		{
			Functions.log (string.Format ("Starting Miner Proxy Server @ {0}:{1}", IP.ToString(), Port.ToString()), 1);
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint localEndPoint = new IPEndPoint(IP, Port);
			listener.Bind(localEndPoint);
			listener.Listen(BACKLOG);
			
			while (true)
			{
				Socket sock = listener.Accept(); //Unless you mean here when they first connect. <-
				Functions.log (string.Format("Miner {0} connected!", ((IPEndPoint)sock.RemoteEndPoint).Address), 5);
				RequestHandler rh = new RequestHandler(sock, Pool, PoolPort);
				ThreadPool.QueueUserWorkItem(delegate(object o)
				 {
					rh.Handle ();
				 });
				//Thread requestThread = new Thread(new ThreadStart(rh.Handle));
				//requestThread.Start();
			}
		}
 	}
}
