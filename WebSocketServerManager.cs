using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NFS_LightingCtrlSystem_v1
{
    public class WebSocketServerManager
    {
        private class ConnectedClient
        {
            public IWebSocketConnection Socket { get; set; }
            public string Nickname { get; set; }
        }

        private WebSocketServer server;
        private Dictionary<Guid, ConnectedClient> clients = new Dictionary<Guid, ConnectedClient>();

        // 외부에 노출되는 이벤트
        public event EventHandler<string> OnClientConnected;            // (닉네임 : 메시지)
        public event EventHandler<string> OnClientDisconnected;           // (닉네임)
        public event EventHandler<string> OnMessageReceived;              // (닉네임)
        public event EventHandler<List<string>> OnClientListUpdated;      // 전체 닉네임 목록 (선택적 사용)

        public bool Start(int port)
        {
            try
            {
                server = new WebSocketServer($"ws://0.0.0.0:{port}");
                server.Start(socket =>
                {
                    Guid id = socket.ConnectionInfo.Id;

                    // 중복 연결 차단
                    var ip_check = socket.ConnectionInfo.ClientIpAddress;
                    if (clients.Values.Any(c => c.Socket.ConnectionInfo.ClientIpAddress == ip_check))
                    {
                        socket.Close();
                        return;
                    }


                    socket.OnOpen = () =>
                    {
                        var nickname = $"User_{clients.Count + 1}";
                        clients[id] = new ConnectedClient
                        {
                            Socket = socket,
                            Nickname = nickname
                        };

                        socket.Send("CONNECTED_ID:" + id);
                        socket.Send("CONNECTED_NICK:" + nickname);
                        OnClientConnected?.Invoke(this, nickname);
                        NotifyClientListUpdated();
                    };

                    socket.OnClose = () =>
                    {
                        if (clients.TryGetValue(id, out var client))
                        {
                            string nickname = client.Nickname;
                            clients.Remove(id);
                            OnClientDisconnected?.Invoke(this, nickname);
                            NotifyClientListUpdated();
                        }
                    };

                    socket.OnMessage = message =>
                    {
                        if (clients.TryGetValue(id, out var client))
                        {
                            if (message.StartsWith("NICK:"))
                            {
                                string oldNick = client.Nickname;
                                client.Nickname = message.Substring(5);
                                socket.Send($"닉네임 변경됨: {oldNick} → {client.Nickname}");
                                NotifyClientListUpdated();
                                return;
                            }

                            string formatted = $"{client.Nickname} : {message}";
                            OnMessageReceived?.Invoke(this, formatted);
                        }
                    };
                });

                string ip = GetLocalIPv4();
                System.Diagnostics.Debug.WriteLine($"WebSocket 서버 시작됨: ws://{ip}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("서버 시작 실패: " + ex.Message);
                return false;
            }
        }

        public void SendToNickname(string nickname, string message)
        {
            var target = clients.Values.FirstOrDefault(c => c.Nickname == nickname);
            if (target?.Socket?.IsAvailable == true)
                target.Socket.Send(message);
        }

        public void SendToAll(string message)
        {
            foreach (var client in clients.Values)
            {
                if (client.Socket.IsAvailable)
                    client.Socket.Send(message);
            }
        }

        public List<string> GetAllNicknames()
        {
            return clients.Values.Select(c => c.Nickname).ToList();
        }

        private void NotifyClientListUpdated()
        {
            OnClientListUpdated?.Invoke(this, GetAllNicknames());
        }

        public static string GetLocalIPv4()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            return "127.0.0.1";
        }
    }
}
