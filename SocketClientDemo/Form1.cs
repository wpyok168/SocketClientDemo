using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClientDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Client client = new Client();

        private async void Form1_Load(object sender, EventArgs e)
        {
            //Client client = new Client();
            client.OnConnected += () => Console.WriteLine("Connected to server.");
            client.OnDisconnected += () => Console.WriteLine("Disconnected from server.");
            client.OnTextSent += text => Console.WriteLine($"Text sent: {text}");
            client.OnTextReceived += (text, from) => Console.WriteLine($"Text received from {from}: {text}");
            client.OnFileProgress += (fileName, current, total) =>
                Console.WriteLine($"File progress: {fileName} ({current}/{total})");
            client.OnFileSent += fileName => Console.WriteLine($"File sent: {fileName}");

            await client.ConnectAsync("127.0.0.1", 8899);
            await client.SendTextAsync("Hello, Server!");

            //await client.SendFileAsync("example.txt");
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Disconnect();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            await client.SendTextAsync("Hello, SendMsg!");
            await client.SendFileAsync("WPS电信定制版_12.8.2.18205_Setup.exe");
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await client.RequestFileAsync("ReceivedFiles\\WPS电信定制版_12.8.2.18205_Setup.exe");
        }
    }
}
