using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;

namespace PushService
{
	/// <summary>
	/// Class that will run as a Windows Service and its display name is
	/// TCP (Sabre Group Config Transfer Service) in Windows Services.
	/// This service basically start a server on service start 
	/// (on OnStart method) and shutdown the server on the servie stop 
	/// (on OnStop method).
	/// </summary>
	public class PushService : ServiceBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private TCPServer server=null;

		public PushService()
		{
			InitializeComponent();
		}

        /// <summary>
        /// The main entry point for the process
        /// </summary>
		static void Main()
		{
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] { new PushService() };
			ServiceBase.Run(ServicesToRun);
		}

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.ServiceName = "PushService";
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Set things in motion so your service can do its work.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			server = new TCPServer();
			server.StartServer();
		}
 
		/// <summary>
		/// Stop this service.
		/// </summary>
		protected override void OnStop()
		{
			server.StopServer();
			server=null;
		}
	}
}
