using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace PushServiceConsole
{
	/// <summary>
	/// TCPServer is the Server class. When "StartServer" method is called
	/// this Server object tries to connect to a IP Address specified on a port
	/// configured. Then the server start listening for client socket requests.
	/// As soon as a requestcomes in from any client then a Client Socket 
	/// Listening thread will be started. That thread is responsible for client
	/// communication.
	/// </summary>
	public class TCPServer
	{
		/// <summary>
		/// Default Constants.
		/// </summary>
		public static IPAddress DEFAULT_SERVER = IPAddress.Parse("127.0.0.1"); 
		public static int DEFAULT_PORT = 31001;
		public static IPEndPoint DEFAULT_IP_END_POINT = new IPEndPoint(DEFAULT_SERVER, DEFAULT_PORT);

		/// <summary>
		/// Local Variables Declaration.
		/// </summary>
		private TcpListener _server = null;
		private bool _stopServer=false;
		private bool _stopPurging=false;
		private Thread _serverThread = null;
		private Thread _purgingThread = null;
		private ArrayList _socketListenersList = null;

		/// <summary>
		/// Constructors.
		/// </summary>
		public TCPServer()
		{
			Init(DEFAULT_IP_END_POINT);
		}
		public TCPServer(IPAddress serverIP)
		{
			Init(new IPEndPoint(serverIP, DEFAULT_PORT));
		}

		public TCPServer(int port)
		{
			Init(new IPEndPoint(DEFAULT_SERVER, port));
		}

		public TCPServer(IPAddress serverIP, int port)
		{
			Init(new IPEndPoint(serverIP, port));
		}

		public TCPServer(IPEndPoint ipNport)
		{
			Init(ipNport);
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~TCPServer()
		{
			StopServer();
		}

		/// <summary>
		/// Init method that create a server (TCP Listener) Object based on the
		/// IP Address and Port information that is passed in.
		/// </summary>
		/// <param name="ipNport"></param>
		private void Init(IPEndPoint ipNport)
		{
			try
			{
				_server = new TcpListener(ipNport);
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "ipAddress " + ipNport.Address.ToString() + " " + "portNum " + ipNport.Port));
			}
			catch(Exception ex)
			{
				_server=null;
                Trace.TraceError(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), ex.Message));
			}
		}

		/// <summary>
		/// Method that starts TCP/IP Server.
		/// </summary>
		public void StartServer()
		{
            try
            {
                if (_server == null) return;
                _socketListenersList = new ArrayList();
                _server.Start();
                _serverThread = new Thread(new ThreadStart(ServerThreadStart));
                _serverThread.Start();
                _purgingThread = new Thread(new ThreadStart(PurgingThreadStart));
                _purgingThread.Priority = ThreadPriority.Lowest;
                _purgingThread.Start();
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Success Starting."));
            }
            catch (SocketException se)
            {
                Trace.TraceWarning(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), se.Message));
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), ex.Message));
            }
		}

		/// <summary>
		/// Method that stops the TCP/IP Server.
		/// </summary>
		public void StopServer()
		{
            try
            {
                if (_server == null) return;
                _stopServer = true;
                _server.Stop();
                _serverThread.Join(1000);
                if (_serverThread.IsAlive) _serverThread.Abort();
                _serverThread = null;
                _stopPurging = true;
                _purgingThread.Join(1000);
                if (_purgingThread.IsAlive) _purgingThread.Abort();
                _purgingThread = null;
                _server = null;
                StopAllSocketListers();
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Success Stopping."));
            }
            catch (SocketException se)
            {
                Trace.TraceWarning(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), se.Message));
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), ex.Message));
            }
		}


		/// <summary>
		/// Method that stops all clients and clears the list.
		/// </summary>
		private void StopAllSocketListers()
		{
			foreach (TCPSocketListener socketListener in _socketListenersList)
			{
				socketListener.StopSocketListener();
			}
			_socketListenersList.Clear();
			_socketListenersList=null;
		}

		/// <summary>
		/// TCP/IP Server Thread that is listening for clients.
		/// </summary>
		private void ServerThreadStart()
		{
			Socket clientSocket = null;
			TCPSocketListener socketListener = null;
			while(!_stopServer)
			{
				try
				{
					clientSocket = _server.AcceptSocket();
                    Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss")
                        , "Accept Client Address " + IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString()) 
                        + " Port Number " + ((IPEndPoint)clientSocket.RemoteEndPoint).Port.ToString()));
					socketListener = new TCPSocketListener(clientSocket);
					lock(_socketListenersList)
					{
						_socketListenersList.Add(socketListener);
					}
					socketListener.StartSocketListener();
				}
				catch (SocketException se)
				{
					_stopServer = true;
                    Trace.TraceWarning(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), se.Message));
				}
                catch(Exception ex)
                {
                    _stopServer = true;
                    Trace.TraceError(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), ex.Message));
                }
			}
		}

		/// <summary>
		/// Thread method for purging Client Listeneres that are marked for
		/// deletion (i.e. clients with socket connection closed). This thead
		/// is a low priority thread and sleeps for 10 seconds and then check
		/// for any client SocketConnection obects which are obselete and 
		/// marked for deletion.
		/// </summary>
		private void PurgingThreadStart()
		{
			while (!_stopPurging)
			{
				var deleteList = new ArrayList();
				lock(_socketListenersList)
				{
					foreach (TCPSocketListener socketListener in _socketListenersList)
					{
						if (socketListener.IsMarkedForDeletion())
						{
							deleteList.Add(socketListener);
							socketListener.StopSocketListener();
						}
					}
					for(int i=0; i<deleteList.Count; ++i)
					{
						_socketListenersList.Remove(deleteList[i]);
					}
                    Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Current Client Accept Count " + _socketListenersList.Count));
				}
				deleteList=null;
				Thread.Sleep(10000);
			}
		}
	}
}