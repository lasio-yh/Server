using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PushServiceConsole
{
	/// <summary>
	/// Summary description for TCPSocketListener.
	/// </summary>
	public class TCPSocketListener
	{
		/// <summary>
		/// Variables that are accessed by other classes indirectly.
		/// </summary>
		private Socket _clientSocket = null;
		private bool _stopClient = false;
		private Thread _clientListenerThread = null;
		private bool _markedForDeletion = false;
		private DateTime _lastReceiveDateTime;
		private DateTime _currentReceiveDateTime;

        private enum COMMAND { PROCESS1 = 0x01, PROCESS2 = 0x02, PROCESS3 = 0x03, PROCESS4 = 0x04, PROCESS5 = 0x05 }
		
		/// <summary>
		/// Client Socket Listener Constructor.
		/// </summary>
		/// <param name="clientSocket"></param>
		public TCPSocketListener(Socket clientSocket)
		{
			_clientSocket = clientSocket;
		}

		/// <summary>
		/// Client SocketListener Destructor.
		/// </summary>
		~TCPSocketListener()
		{
			StopSocketListener();
		}

		/// <summary>
		/// Method that starts SocketListener Thread.
		/// </summary>
		public void StartSocketListener()
		{
            if (_clientSocket == null) return;
            _clientListenerThread = new Thread(new ThreadStart(SocketListenerThreadStart));
            _clientListenerThread.Start();
            Trace.TraceInformation("Compleate Listenering.");
		}

		/// <summary>
		/// Thread method that does the communication to the client. This 
		/// thread tries to receive from client and if client sends any data
		/// then parses it and again wait for the client data to come in a
		/// loop. The recieve is an indefinite time receive.
		/// </summary>
		private void SocketListenerThreadStart()
		{
			var size = 0;
			var buffer = new Byte[0xff];
			_lastReceiveDateTime = DateTime.Now;
			_currentReceiveDateTime = DateTime.Now;
            var timeOutTimer = new Timer(new TimerCallback(CheckedClientTimeOut), null, 15000, 15000);
			while (!_stopClient)
			{
                try
                {
                    size = _clientSocket.Receive(buffer);
                    _clientSocket.Send(buffer);
                    Trace.TraceInformation("/" + size.ToString());
                    if (size < 0) continue;
                    _currentReceiveDateTime = DateTime.Now;
                    //var data = (PacketDTO)PacketConvert.ByteToStructure(buffer, typeof(PacketDTO));
                    //Trace.TraceInformation(string.Format("{0} | {1} | {2} byte | {3} | {4} | {5} | {6}", "Receive", _currentReceiveDateTime.ToString("hh:mm:ss"), buffer.Length, data.COMMAND, data.HEADER, data.DATA, data.AUTH));
                    //PacketConvert.StructureByDespose(data);
                    //OnAction(data, size);
                }
                catch (SocketException se)
                {
                    _stopClient = true;
                    _markedForDeletion = true;
                    Trace.TraceWarning(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), se.Message));
                }
                catch (Exception ex)
                {
                    _stopClient = true;
                    _markedForDeletion = true;
                    Trace.TraceError(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), ex.Message));
                }
			}
            timeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            timeOutTimer = null;
		}

		/// <summary>
		/// Method that stops Client SocketListening Thread.
		/// </summary>
		public void StopSocketListener()
		{
            if (_clientSocket == null) return;
            _stopClient = true;
            _clientSocket.Close();
            _clientListenerThread.Join(1000);
            if (_clientListenerThread.IsAlive) _clientListenerThread.Abort();
            _clientListenerThread = null;
            _clientSocket = null;
            _markedForDeletion = true;
            GC.Collect();
		}

		/// <summary>
		/// Method that returns the state of this object i.e. whether this
		/// object is marked for deletion or not.
		/// </summary>
		/// <returns></returns>
		public bool IsMarkedForDeletion()
		{
			return _markedForDeletion;
		}

		/// <summary>
		/// This method parses data that is sent by a client using TCP/IP.
		/// As per the "Protocol" between client and this Listener, client 
		/// sends each line of information by appending "CRLF" (Carriage Return
		/// and Line Feed). But since the data is transmitted from client to 
		/// here by TCP/IP protocol, it is not guarenteed that each line that
		/// arrives ends with a "CRLF". So the job of this method is to make a
		/// complete line of information that ends with "CRLF" from the data
		/// that comes from the client and get it processed.
		/// </summary>
		/// <param name="byteBuffer"></param>
		/// <param name="size"></param>
        private void OnAction(PacketDTO packetObj, int size)
		{
            try
            {
                switch (packetObj.COMMAND)
                {
                    case 0x01:
                        {
                            //dataPacket Process add in
                            //SendResponse();
                            break;
                        }
                    case 0x02:
                        {
                            //dataPacket Process add in
                            StopSocketListener();
                            break;
                        }
                    case 0x03:
                        {
                            //dataPacket Process add in
                            //SendResponse();
                            break;
                        }
                    case 0x04:
                        {
                            //dataPacket Process add in
                            //SendResponse();
                            break;
                        }
                    case 0x05:
                        {
                            //dataPacket Process add in
                            //SendResponse();
                            break;
                        }
                    default:
                        break;
                }
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

        private void SendResponse(int command, int headerLength, int dataLength, string header, string data, string auth)
        {
            try
            {
                var packet = new PacketDTO(0x01, command, headerLength, dataLength, header, data, auth, 0x01);
                var response = PacketConvert.StructureToByte(packet);
                _clientSocket.Send(response);
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Send Response Length " + response.Length + " byte."));
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
		/// Method that checks whether there are any client calls for the
		/// last 15 seconds or not. If not this client SocketListener will
		/// be closed.
		/// </summary>
		/// <param name="o"></param>
		private void CheckedClientTimeOut(object o)
		{
            if (_lastReceiveDateTime.Equals(_currentReceiveDateTime)) this.StopSocketListener();
            else _lastReceiveDateTime = _currentReceiveDateTime;
		}
	}
}