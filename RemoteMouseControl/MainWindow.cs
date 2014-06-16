using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteMouseControl {
    public partial class MainWindow : Form {
        private Task listenerTask;

        //DLL used to set cursor´s position
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        public MainWindow() {
            InitializeComponent();

            //Receiver backgroud task
            listenerTask = Task.Factory.StartNew(ListenMessages, TaskCreationOptions.LongRunning);
            listenerTask.ContinueWith(t => { }, TaskScheduler.FromCurrentSynchronizationContext());

            //Print machine IP
            IpLabel.Text = GetDefaultIP();
        }

        private static string GetDefaultIP() {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            var gateway = (adapters.Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetIPProperties().GatewayAddresses.First())
                ).FirstOrDefault().Address;

            foreach (NetworkInterface adapter in adapters) {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                if (properties.GatewayAddresses.Count <= 0) continue;
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    properties.GatewayAddresses.First().Address == gateway) {
                    foreach (var x in properties.UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)) {
                        return x.Address.ToString();
                    }
                }
            }

            return "Null IP";
        }

        private void ListenMessages() {
            while (true) {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var iep = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.Port);
                sock.Bind(iep);
                var ep = (EndPoint)iep;

                var data = new byte[32];
                int recv = sock.ReceiveFrom(data, ref ep);

                string stringData = Encoding.UTF8.GetString(data, 0, recv);
                //MessageBox.Show(stringData);

                string[] arrayData = stringData.Split(';');


                int x = int.Parse(arrayData[0]);
                int y = int.Parse(arrayData[1]);

                //MessageBox.Show("X:" + x + " Y:" + y);

                //MessageBox.Show("X:" + MousePosition.X + " Y:" + MousePosition.Y);

                int newX = MousePosition.X + Screen.PrimaryScreen.Bounds.Width * x / 100;
                int newY = MousePosition.Y + Screen.PrimaryScreen.Bounds.Height * y / 100;

                //MessageBox.Show("X:" + newX + " Y:" + newY);

                //xLabel.Text = "Delta X: " + x;
                //yLabel.Text = "Delta Y: " + y;

                BeginInvoke(new Action(() => SetCursorPos(newX, newY)));

                sock.Close();
            }
        }
    }
}
