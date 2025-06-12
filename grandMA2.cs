using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NFS_LightingCtrlSystem_v1
{
    public class grandMA2
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public bool IsConnected => client != null && client.Connected;

        public event Action<string> MessageReceived;
        public event Action Disconnected;

        private readonly BlockingCollection<string> commandQueue = new BlockingCollection<string>();
        private CancellationTokenSource cts;

        private bool loginCompleted = false;

        private string expectedUsername = "";

        public async Task<bool> ConnectAsync(string ip, string username, string password = "")
        {
            try
            {
                expectedUsername = username;

                client = new TcpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                await client.ConnectAsync(ip, 30000);

                stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.ASCII);
                writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                _ = Task.Run(() => ReceiveLoop());
                _ = Task.Run(() => CommandLoop());

                await Task.Delay(200);
                await writer.WriteLineAsync($"login {username}");
                if (!string.IsNullOrEmpty(password))
                    await writer.WriteLineAsync(password);

                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public void SendCommand(string command)
        {
            if (IsConnected)
                commandQueue.Add(command.Trim());
        }

        private async Task CommandLoop()
        {
            cts = new CancellationTokenSource();

            try
            {
                foreach (var cmd in commandQueue.GetConsumingEnumerable(cts.Token))
                {
                    if (writer != null)
                        await writer.WriteLineAsync(cmd);
                    await Task.Delay(50);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (IsConnected)
                {
                    string line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue; // 빈 줄, 공백만 있는 줄은 무시

                    string cleanLine = RemoveAnsiEscapeCodes(line);

                    if (!loginCompleted && cleanLine.Contains($"Logged in as User '{expectedUsername}'"))
                        loginCompleted = true;

                    if (loginCompleted)
                        MessageReceived?.Invoke(cleanLine);
                }
            }
            catch { }
            finally
            {
                loginCompleted = false;
                Disconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            cts?.Cancel();
            client?.Close();
            client = null;
            loginCompleted = false;
        }

        private string RemoveAnsiEscapeCodes(string input)
        {
            return Regex.Replace(input, @"\x1B\[[0-9;]*[A-Za-z]", "");
        }
    }
}
