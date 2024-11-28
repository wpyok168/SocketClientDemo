using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClientDemo
{
    public class Client
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool isconect = true;

        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnTextSent;
        public event Action<string, string> OnTextReceived;
        public event Action<string, long, long> OnFileProgress;
        public event Action<string> OnFileSent;
        public event Action<string> OnillegalConnected;


        public async Task<bool> ConnectAsync(string host, int port = 8899)
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                OnConnected?.Invoke();
                _ = Task.Run(() => ReceiveDataAsync());
                return isconect = true;
            }
            catch (Exception ex)
            {
                OnillegalConnected?.Invoke($"{ex.Message}");
                //_client.Close();    
                //throw;
                return isconect = false;
            }

        }

        public void Disconnect()
        {
            if (!isconect) return;
            _stream.Close();
            _client.Close();
            OnDisconnected?.Invoke();
        }

        private async Task ReceiveDataAsync()
        {
            if (!isconect) return;

            var buffer = new byte[8192];

            while (_client.Connected)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var message = JsonSerializer.Deserialize<Message>(json);

                switch (message.Type)
                {
                    case "text":
                        OnTextReceived?.Invoke(message.Text, "Server");
                        break;
                    case "file":
                        await ReceiveFileAsync(message);
                        break;
                        // 可扩展接收文件或其他类型处理
                }
            }
        }

        public async Task SendTextAsync(string text)
        {
            if (!isconect) return;
            var message = new Message { Type = "text", Text = text };
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            OnTextSent?.Invoke(text);
        }

        public async Task SendFileAsync(string filePath)
        {
            if (!isconect) return;
            var fileName = Path.GetFileName(filePath);
            string filePath1 = filePath.Equals(fileName) ? null:filePath;
            var fileSize = filePath.Equals(fileName) ? new FileInfo(fileName).Length : new FileInfo(filePath).Length;
            var metadata = new Message { Type = "file", FileName = fileName, FileSize = fileSize, FilePath = filePath1 };

            var metadataJson = JsonSerializer.Serialize(metadata);
            var metadataBuffer = Encoding.UTF8.GetBytes(metadataJson);

            await _stream.WriteAsync(metadataBuffer, 0, metadataBuffer.Length);

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[8192];
                long bytesSent = 0;
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await _stream.WriteAsync(buffer, 0, bytesRead);
                    bytesSent += bytesRead;
                    OnFileProgress?.Invoke(fileName, bytesSent, fileSize);
                }

                OnFileSent?.Invoke(fileName);
            };
        }
        private async Task ReceiveFileAsync(Message message)
        {
            //var filePath = Path.Combine("ReceivedFiles", message.FileName);
            //Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var filePath = message.FilePath ?? Path.Combine("ReceivedFiles", message.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            long existingSize = 0;
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            {
                var buffer = new byte[8192];

                while (existingSize < message.FileSize)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    fileStream.Write(buffer, 0, bytesRead);
                    existingSize += bytesRead;

                    OnFileProgress?.Invoke(message.FileName, existingSize, message.FileSize);
                }

                Console.WriteLine($"File {message.FileName} received.");
            };
        }
        public async Task RequestFileAsync(string filepath)
        {
            string fileName=Path.GetFileName(filepath);
            filepath = fileName.Equals(filepath) ? "ReceivedFiles\\"+fileName : filepath;
            //filepath = fileName.Equals(filepath) ? null : filepath;
            var message = new Message { Type = "request_file", FileName = fileName, FilePath=filepath };
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
    public class Message
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
    }
}
