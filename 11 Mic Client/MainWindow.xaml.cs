using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace _11_Mic_Client
{
    public sealed partial class MainWindow : Window
    {
        private TcpClient client;
        private WaveOutEvent audioPlayer;
        private BufferedWaveProvider audioBuffer;

        public MainWindow()
        {
            InitializeComponent();
            LoadServerIp();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverIp = ServerIpTextBox.Text;
                int serverPort = int.Parse(ServerPortTextBox.Text);

                StatusText.Text = "Connecting to server...";

                client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);

                StatusText.Text = "Connected to server!";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;

                // 音声データの受信処理を開始
                ReceiveAudioData();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null && client.Connected)
            {
                client.Close();
                StatusText.Text = "Disconnected from server.";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
            }
        }

        private void SaveServerIp()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["ServerIp"] = ServerIpTextBox.Text;
        }

        private void LoadServerIp()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("ServerIp"))
            {
                ServerIpTextBox.Text = localSettings.Values["ServerIp"].ToString();
            }
        }

        private async void ReceiveAudioData()
        {
            NetworkStream stream = client.GetStream();
            WaveFormat waveFormat = new WaveFormat(44100, 16, 1);
            audioPlayer = new WaveOutEvent();
            audioBuffer = new BufferedWaveProvider(waveFormat);
            audioPlayer.Init(audioBuffer);
            audioPlayer.Play();

            while (client.Connected)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;

                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (IOException ex)
                {
                    // ネットワーク接続が中断された場合の処理
                    StatusText.Text = $"Error: {ex.Message}";
                    break;
                }

                if (bytesRead == 0)
                {
                    // 接続が切断された場合
                    break;
                }

                // 受信した音声データを処理する
                audioBuffer.AddSamples(buffer, 0, bytesRead);
            }

            client.Close();
            StatusText.Text = "Disconnected from server.";
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }

        private void ServerIpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerIp();
        }
    }
}
