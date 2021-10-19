using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
namespace Communication
{
    /*   
     *   ʹ��ע��:����ͬһ��IP��������ͻ������⣬���η�װû��������(Ĭ�ϴ����������ӽ�����)
     */
    //�ͻ���������
    public delegate void AcceptClientEventHandler(TcpClient client);
    //�ͻ��˽�������
    public delegate void ReciveClientEventHandler(TcpClient client,string strRecvData);
    //�ͻ��˶Ͽ�
    public delegate void CloseClientEventHandler(TcpClient client);

    public class Communication_TcpServer
    {
        //����������������IP
        private string _serverIp = "127.0.0.1";
        //�����������˿�
        private int _listenerPort = 10010;
        //��������
        private TcpListener _listenerInstance = null;
        //�����߳�
        private Thread _threadListenClient = null;
        //�ͻ��˶�������
        public Dictionary<TcpClient, Thread> _clientInstanceDic = new Dictionary<TcpClient, Thread>();
        //������������־
        public bool _isListenning = true;

        //�ͻ��������¼�
        public event AcceptClientEventHandler acceptClientEvent;
        //�ͻ��˷���Ϣ�¼�
        public event ReciveClientEventHandler reciveClientEvent;
        //�ͻ��˶Ͽ��¼�
        public event CloseClientEventHandler closeClientEvent;

        public Communication_TcpServer(string Ip,int nPort)
        {
            _listenerPort = nPort;
            _serverIp = Ip;
        }

        public void  ListenClients()
        {
            _clientInstanceDic.Clear();
            _threadListenClient=new Thread(AcceptClients);
            _threadListenClient.IsBackground=true;
            _threadListenClient.Start();
            _isListenning = true;
        }

        private void AcceptClients()
        {
            try
            {
                if (!Regex.IsMatch(_serverIp, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$") || _listenerPort > 65535)
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("IP���߶˿ڴ���:{0}:{1}", _serverIp, _listenerPort), " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return ;
                }
                //��ȡ����IP
                _listenerInstance = new TcpListener(IPAddress.Parse(_serverIp), _listenerPort);
                _listenerInstance.Start();//��ʼ����
            }
            catch (SocketException se)
            {
                System.Windows.Forms.MessageBox.Show(se.Message, " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            try
            {
                while (_isListenning)
                {
                    TcpClient Client = _listenerInstance.AcceptTcpClient();
                    //�ͻ���������
                    if (acceptClientEvent!=null)
                    {
                        acceptClientEvent(Client);
                    }
                    
                    Thread tmpClientThread = new Thread(new ParameterizedThreadStart(ReceiveClient));
                    _clientInstanceDic.Add(Client, tmpClientThread);

                    tmpClientThread.IsBackground = true;
                    tmpClientThread.Name = "client handle";

                    tmpClientThread.Start(Client);

                }
            }
            catch (SocketException ex)
            {
                Trace.WriteLine(ex.Message);
                System.Windows.Forms.MessageBox.Show(ex.Message, " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ReceiveClient(object clientObject)
        {
            TcpClient client = clientObject as TcpClient;
            NetworkStream netStream = null;

            try
            {
                netStream = client.GetStream();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            try
            {
                while (_isListenning)
                {
                    if ((client.Client.Poll(100, SelectMode.SelectRead) && (client.Client.Available == 0) || !client.Connected))
                    {
                        //�Ͽ�
                        _clientInstanceDic.Remove(client);
                        if (closeClientEvent!=null)
                        {
                            closeClientEvent(client);
                        }
                       
                        return;
                    }
                    byte[] receiveBuffer = new byte[client.ReceiveBufferSize];
                    int nRecvLen = netStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                    if (nRecvLen <= 0)
                    {
                        //�Ͽ�
                        _clientInstanceDic.Remove(client);
                        if (closeClientEvent != null)
                        {
                            closeClientEvent(client);
                        }
                        return;
                    }
                    string Data = System.Text.Encoding.Default.GetString(receiveBuffer);
                    Data = Data.Replace("\0","");
                    reciveClientEvent(client, Data);
                }
            }
//             catch (System.IO.IOException ex)
//             {
//                 Trace.WriteLine(ex.Message);
//                 _clientInstanceDic.Remove(client);
//                 if (closeClientEvent != null)
//                 {
//                     closeClientEvent(client);
//                 }
//             }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                //System.Windows.Forms.MessageBox.Show(ex.Message, " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                _clientInstanceDic.Remove(client);
                if (closeClientEvent != null)
                {
                    closeClientEvent(client);
                }
            }
        }

        public bool Write(string strIP, string strMessage)
        {
            try
            {
                NetworkStream netStream = null;
                foreach (TcpClient tc in _clientInstanceDic.Keys)
                {
                    if (((IPEndPoint)tc.Client.RemoteEndPoint).Address.ToString() == strIP)
                    {
                        if (_clientInstanceDic[tc].IsAlive)
                        {
                            netStream = tc.GetStream();
                            break;
                        }
                    }
                }
                if (netStream == null)
                {
                    Trace.WriteLine("client thread is dead");
                    return false;
                }
                netStream.Write(Encoding.Default.GetBytes(strMessage), 0, strMessage.Length);
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, " error !", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
        public void Close()
        {
            _isListenning = false;
            foreach (TcpClient tc in _clientInstanceDic.Keys)
            {
                tc.Close();
            }
            _clientInstanceDic.Clear();
        }
    }
}
