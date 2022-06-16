using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SseNamedPipeTest
{
    public class SseNamedPipeClient
    {
        private MessageWorker _worker;
        private string _pipeName;
        private bool _isConnected;
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private NamedPipeClientStream _pipe;
        private PipeMessage _pipeMessage;
        private StreamWriter _writer;

        public event EventHandler Connect;
        public event EventHandler DisConnect;
        public event EventHandler<string> MessageReceived; 

        public SseNamedPipeClient(string pipeName)
        {
            _pipeName = pipeName;
            _isConnected = false;
            _worker = new MessageWorker();
            NewPipe();
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += RunWorkerCompleted;
        }

        public void SendMessage(string message)
        {
            //todo lm 失败的逻辑注意一下
            if(string.IsNullOrEmpty(message)) return;
            _worker.InsertWorkItem(() =>
            {
                if (_pipe.IsConnected)
                {
                    _isConnected = true;
                    _writer.WriteLine(message);
                    _writer.Flush();
                }
                else
                {
                    _isConnected = false;
                    throw new Exception("Not connected to pipe.");
                }
            });
        }

        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _isConnected = _pipe.IsConnected;
            if (_isConnected)
            {
                _writer = new StreamWriter(_pipe);
                OnConnect();
                BeginRead();
            }
            else
            {
                Close();
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_pipe.IsConnected)
            {
                try
                {
                    _isConnected = _pipe.IsConnected;
                    _pipe.Connect(2000);
                }
                catch (IOException iox)
                {
                    Debug.WriteLine(iox.Message);
                    _isConnected = false;
                    if (iox.Message.Contains("expired")) continue;
                    throw;
                }
                catch (ObjectDisposedException ode)
                {
                    Debug.WriteLine(ode.Message);
                    Close();
                    break;
                }
            }
        }

        private void NewPipe()
        {
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }

        private void BeginRead()
        {
            _pipeMessage = new PipeMessage();
            _pipe.BeginRead(_pipeMessage.MessageBytes, 0, PipeMessage.MessageBufferSize, AsyncReadMessageCallback,
                _pipe);
        }

        private void AsyncReadMessageCallback(IAsyncResult result)
        {
            try
            {
                _pipe.EndRead(result);
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine(ae.Message);
            }

            if (!_pipeMessage.IsNullOrEmpty())
            {
                OnMessageReceived(_pipeMessage.Message);
                _pipeMessage = new PipeMessage();
            }

            if (_pipe.IsConnected)
            {
                BeginRead();
            }
            else
            {
                Close();
                Open();
            }
        }

        public void Open()
        {
            if (!_isConnected && !_backgroundWorker.IsBusy) _backgroundWorker.RunWorkerAsync();
        }

        private void Close()
        {
            _isConnected = false;
            if (_pipe.IsConnected)
            {
                _pipe.WaitForPipeDrain();
                _pipe.Close();
                _writer?.Close();
                _writer?.Dispose();
            }

            //注意一下重试逻辑
            OnDisconnect();
            NewPipe();
        }

        private void OnConnect()
        {
            Connect?.Invoke(this, EventArgs.Empty);
        }

        private void OnDisconnect()
        {
            DisConnect?.Invoke(this, EventArgs.Empty);
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }
    }
}
