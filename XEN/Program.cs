using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using KeyAuth;
using Microsoft.Win32;

namespace XEN
{
    static class Program
    {
        #region Imports
        [SuppressUnmanagedCodeSecurity]
		delegate object ExecuteAssembly(object sender, object[] parameters);

		private const int MF_BYCOMMAND = 0x00000000;
		public const int SC_CLOSE = 0xF060;
		public const int SC_MINIMIZE = 0xF020;
		public const int SC_MAXIMIZE = 0xF030;
		public const int SC_SIZE = 0xF000;

		[DllImport("user32.dll")]
		public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		private static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		#endregion

		#region Run Pe
		public static void Run(byte[] buffer)
		{
			int e_lfanew = BitConverter.ToInt32(buffer, 0x3c);
			Buffer.SetByte(buffer, e_lfanew + 0x382, 2);

			object[] parameters = null;

			Assembly assembly = Thread.GetDomain().Load(buffer);
			MethodInfo entrypoint = assembly.EntryPoint;
			if (entrypoint.GetParameters().Length > 0)
			{
				parameters = new object[] { new string[] { null } };
			}

			Thread assemblyExecuteThread = new Thread(() =>
			{
				Thread.BeginThreadAffinity();
				Thread.BeginCriticalRegion();

				ExecuteAssembly executeAssembly = new ExecuteAssembly(entrypoint.Invoke);
				executeAssembly(null, parameters);

				Thread.EndCriticalRegion();
				Thread.EndThreadAffinity();
			});

			if (parameters != null)
			{
				if (parameters.Length > 0)
				{
					assemblyExecuteThread.SetApartmentState(ApartmentState.STA);
				}
				else
				{
					assemblyExecuteThread.SetApartmentState(ApartmentState.MTA);
				}
			}

			assemblyExecuteThread.Start();
		}
		#endregion

		#region Random String Generator
		static string Random_String()
		{
			string str = null;

			Random random = new Random();
			for (int i = 0; i < 5; i++)
			{
				str += Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65))).ToString();
			}
			return str;
		}
		#endregion

		#region Toggle Console Window
		const int SW_HIDE = 0;
		const int SW_SHOW = 5;

		public static void Toggle_Window(bool enable)
        {
			var handle = GetConsoleWindow();

			if (enable)
            {
				ShowWindow(handle, SW_SHOW);
			}
			else if (!enable)
            {
				ShowWindow(handle, SW_HIDE);
			}
        }
		#endregion

		public static api KeyAuthApp = new api(name: "Xen", ownerid: "2jSWukruyc", secret: "7882ec42d9bc29d70653431eac048543da49c15455ab3530d1d07661d021863d", version: "1.0");

		static void Main()
        {
			Console.Title = Random_String();

			Toggle_Window(false);

			IntPtr handle = GetConsoleWindow();
			IntPtr sysMenu = GetSystemMenu(handle, false);

			DeleteMenu(sysMenu, SC_CLOSE, MF_BYCOMMAND);
			DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
			DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
			DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);

			Console.BufferWidth = Console.WindowWidth = 50;
			Console.BufferHeight = Console.WindowHeight;

			Toggle_Window(true);

			Console.Write(" Connecting to loader please wait...");
			KeyAuthApp.init();

			Console.Write("\n Checking for loader updates...");

            #region Auto Update
            if (KeyAuthApp.response.message == "invalidver")
            {
				Console.Write("\n Update found please wait...");

				if (!string.IsNullOrEmpty(KeyAuthApp.app_data.downloadLink))
                {
					Console.Write("\n Downloading latest loader...");

					WebClient webClient = new WebClient();

					string name = Random_String();

					webClient.DownloadFile(KeyAuthApp.app_data.downloadLink, Application.StartupPath + "\\" + name + ".exe");

					Process.Start(Application.StartupPath + "\\" + name + ".exe");

					Process.Start(new ProcessStartInfo()
					{
						Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + Application.ExecutablePath + "\"",
						WindowStyle = ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
						FileName = "cmd.exe"
					});

					Environment.Exit(0);
				}
			}
			#endregion

			Console.Write("\n Latest version is running...");

			Console.Write("\n\n Application Information:");
			Console.Write("\n\n Total Users: " + KeyAuthApp.app_data.numUsers);
			Console.Write("\n Application Version: " + KeyAuthApp.app_data.version);

			KeyAuthApp.check();

			Console.Write($"\n\n Session Validation: {KeyAuthApp.response.message}");

			Console.WriteLine("\n\n [1] Login");
			Console.WriteLine(" [2] Register");
			Console.WriteLine(" [3] Upgrade Key");
			Console.Write("\n Input: ");

			string username;
			string password;
			string key;

			int option = int.Parse(Console.ReadLine());

			Console.Clear();

			Console.Write(" Xen Loader - Version " + KeyAuthApp.app_data.version );
			Console.Write("\n\n");

			switch (option)
            {
				case 1:
					Console.Write(" Username: ");
					username = Console.ReadLine();
					Console.Write(" Password: ");
					password = Console.ReadLine();
					KeyAuthApp.login(username, password);
					break;

				case 2:
					Console.Write(" Username: ");
					username = Console.ReadLine();
					Console.Write(" Password: ");
					password = Console.ReadLine();
					Console.Write(" License: ");
					key = Console.ReadLine();
					KeyAuthApp.register(username, password, key);
					break;
				case 3:
					Console.Write(" Username: ");
					username = Console.ReadLine();
					Console.Write(" License: ");
					key = Console.ReadLine();
					KeyAuthApp.upgrade(username, key);
					break;
				default:
					Console.Write(" Incorrect option selected, closing now...");
					Thread.Sleep(2500);
					Environment.Exit(0);
					break; 
			}

			if (!KeyAuthApp.response.success)
			{
				Console.Write("\n\n " + KeyAuthApp.response.message);
				Thread.Sleep(1500);
				Environment.Exit(0);
			}

			Console.Write("\n User data:");
			Console.Write("\n Username: " + KeyAuthApp.user_data.username);
			Console.Write("\n IP address: " + KeyAuthApp.user_data.ip);
			Console.Write("\n Hardware-Id: \n " + KeyAuthApp.user_data.hwid);
			Console.Write("\n Created at: " + UnixTimeToDateTime(long.Parse(KeyAuthApp.user_data.createdate)));

			if (!String.IsNullOrEmpty(KeyAuthApp.user_data.lastlogin)) 
				Console.Write("\n Last Login At: " + UnixTimeToDateTime(long.Parse(KeyAuthApp.user_data.lastlogin)));

			Console.Write("\n\n Getting loader ready please wait... ");

			Thread.Sleep(3500);

			Console.Clear();

			Console.Write(" Xen Loader - Version " + KeyAuthApp.app_data.version);
			Console.Write("\n\n");

			KeyAuthApp.check();

			Console.Write($" Session Validation: {KeyAuthApp.response.message}");

			Console.Write("\n\n [*] Downloading files please wait...");

			byte[] result = KeyAuthApp.download("410684");

			Console.Write("\n [*] Running files...");

			Run(result);

			Console.Write("\n [*] Done!");

			Console.Write("\n\n [*] Cleaning up please wait...");

			Thread.Sleep(1000);

			Toggle_Window(false);
		}

		public static DateTime UnixTimeToDateTime(long unixtime)
		{
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
			dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
			return dtDateTime;
		}
	}
}
