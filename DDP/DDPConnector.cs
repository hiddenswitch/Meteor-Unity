using System;
using WebSocket4Net;
using Net.DDP.Client.Messages;
using UnityEngine;

namespace Net.DDP.Client
{
    public class LiveConnection
    {
        private WebSocket _socket;
        private string _url=string.Empty;
        private int _isWait = 0;
        private LiveData _client;

        public LiveConnection(LiveData client)
        {
            this._client = client;
        }

        public void Connect(string url)
        {
            _url = "ws://" + url + "/websocket";
            _socket = new WebSocket(_url);
            _socket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(socket_MessageReceived);
            _socket.Opened += new EventHandler(_socket_Opened);
            _socket.Open();
            _isWait = 1;
            this.Wait();
        }

        public void Close()
        {
			if (_socket != null)
			{
				_socket.Close();
			}
        }

        public void Send(string message)
        {
			Debug.Log (message);
            _socket.Send(message);
        }

        void _socket_Opened(object sender, EventArgs e)
        {
            this.Send(ConnectMessage.connectMessage);
            _isWait = 0;
        }

        void socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            this._client.QueueMessage(e.Message);
        }

        private void Wait()
        {
            while (_isWait != 0)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

    }
}
