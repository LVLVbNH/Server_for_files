using System;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Text;

namespace Server_Test
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static TextBlock box;

        public MainWindow()
        {
            InitializeComponent();
            box = textBlock;
        }


        private void button_Click(object sender, RoutedEventArgs e)
        {   
            Server.Start();
            box.Text += "\n[" + DateTime.Now.ToString("HH:mm:ss") + "] Server started";
            button1.IsEnabled = true;
            button.IsEnabled = false;
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Server.Stop();
            button.IsEnabled = true;
            button1.IsEnabled = false;
        }




    }





    static class Server
    {
        private static TcpListener listener;
        private static bool read = false;
        private static TcpClient client;
        private static CancellationTokenSource cancellation = new CancellationTokenSource();

        static public async void Start()
        {
            listener = new TcpListener(IPAddress.Parse("192.168.0.103")/*IPAddress.Any*/, 12333);
            listener.Start();
            read = true;
            cancellation = new CancellationTokenSource();
            try
            {
                while (read)
                {
                    client = await Task.Run(() => listener.AcceptTcpClientAsync().WithCancellation(cancellation.Token));
                    RequestProc(client);
                }
            }
            catch (TaskCanceledException exp)
            {
                listener.Stop();
                MainWindow.box.Text += "\n[" + DateTime.Now.ToString("HH:mm:ss") + "] Theard canceled ";
            }
        }

        private static async void RequestProc(TcpClient NewClient)
        {
            int bitecount = 0;
            var stream = NewClient.GetStream();
            byte[] bytes = new byte[100];
            int bytesRead = stream.Read(bytes, 0, 100);
            string incText = Encoding.UTF8.GetString(bytes).Split(';')[0];
            MainWindow.box.Text += "\n[" + DateTime.Now.ToString("HH:mm:ss") +"] Incomiog transsmition..";
            byte[] buffer = new byte[1024];
            var output = File.Create(incText);
            while((bitecount = await stream.ReadAsync(buffer,0,1024))>0)
                await output.WriteAsync(buffer, 0, bitecount);
            output.Close();
            MainWindow.box.Text += "\n[" + DateTime.Now.ToString("HH:mm:ss") + "] File \"" +incText+ "\" created";
            stream.Close();
        }




        static public void Stop()
        {
            read = false;
            cancellation.Cancel();
            //client.Close();
            //listener.Stop();
            
        }



        static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }


    }
}
