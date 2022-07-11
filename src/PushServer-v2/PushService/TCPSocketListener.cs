using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PushService
{
    /// <summary>
    /// Summary description for TCPSocketListener.
    /// </summary>
    public class TCPSocketListener
    {
        public enum STATE { PROCESS1, PROCESS2, PROCESS3, PROCESS4, PROCESS5 };

        /// <summary>
        /// Variables that are accessed by other classes indirectly.
        /// </summary>
        private Socket _clientSocket = null;
        private bool _stopClient = false;
        private Thread _clientListenerThread = null;
        private bool _markedForDeletion = false;

        /// <summary>
        /// Working Variables.
        /// </summary>
        private STATE _processState = STATE.PROCESS1;
        private DateTime _lastReceiveDateTime;
        private DateTime _currentReceiveDateTime;

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
            var buffer = new Byte[256];
            _lastReceiveDateTime = DateTime.Now;
            _currentReceiveDateTime = DateTime.Now;
            var timeOut = new Timer(new TimerCallback(CheckClientTimeOut), null, 15000, 15000);
            while (!_stopClient)
            {
                try
                {
                    size = _clientSocket.Receive(buffer);
                    _currentReceiveDateTime = DateTime.Now;
                    Trace.WriteLine(_currentReceiveDateTime + " | " + size + " byte.");
                    ParseReceiveBuffer(buffer, size);
                }
                catch (SocketException se)
                {
                    _stopClient = true;
                    _markedForDeletion = true;
                    Trace.TraceWarning(se.Message);
                }
                catch (Exception ex)
                {
                    _stopClient = true;
                    _markedForDeletion = true;
                    Trace.TraceError(ex.Message);
                }
            }
            timeOut.Change(Timeout.Infinite, Timeout.Infinite);
            timeOut = null;
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
            if (_clientListenerThread.IsAlive)
            {
                _clientListenerThread.Abort();
            }
            _clientListenerThread = null;
            _clientSocket = null;
            _markedForDeletion = true;
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
        private void ParseReceiveBuffer(byte[] buffer, int size)
        {
            var stream = Encoding.UTF8.GetString(buffer, 0, size);
            Trace.TraceInformation("Recevice Message : " + stream);
            _processState = STATE.PROCESS1;
            RunProcess(stream);
        }

        /// <summary>
        /// Method that Process the client data as per the protocol. 
        /// The protocol works like this. 
        /// 1. Process1
        /// 
        /// 2. Process2
        /// 
        /// 3. Process3
        /// 
        /// 4. Process4
        /// 
        /// 5. Process5
        /// </summary>
        /// <param name="response"></param>
        private void RunProcess(string buffer)
        {
            try
            {
                Trace.TraceInformation("Run Processing.");
                switch (_processState)
                {
                    case STATE.PROCESS1:
                        SendResponse(buffer);
                        break;
                    case STATE.PROCESS2:
                        SendResponse(buffer);
                        break;
                    case STATE.PROCESS3:
                        SendResponse(buffer);
                        break;
                    case STATE.PROCESS4:
                        SendResponse(buffer);
                        break;
                    case STATE.PROCESS5:
                        SendResponse(buffer);
                        break;
                    default:
                        break;
                }
            }
            catch (SocketException se)
            {
                Trace.TraceWarning(se.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        private void SendResponse(string buffer)
        {
            try
            {
                var response = Encoding.UTF8.GetBytes(buffer);
                Trace.TraceInformation("Response Message : " + buffer + " | " + response.Length + " byte.");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        /// <summary>
        /// Method that checks whether there are any client calls for the
        /// last 15 seconds or not. If not this client SocketListener will
        /// be closed.
        /// </summary>
        /// <param name="o"></param>
        private void CheckClientTimeOut(object o)
        {
            if (_lastReceiveDateTime.Equals(_currentReceiveDateTime))
            {
                this.StopSocketListener();
            }
            else
            {
                _lastReceiveDateTime = _currentReceiveDateTime;
            }
        }
    }
}