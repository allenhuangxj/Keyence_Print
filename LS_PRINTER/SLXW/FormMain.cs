using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

using System.Threading;
using ConfigurationTool;

using Studio_Log;

using PrintReport;
using SYS_DB;
using System.IO;
using Communication;
using System.Net.Sockets;
using System.Net;

namespace SLXW
{
    public partial class FormMain : Form
    {
        //互斥量
        public static Mutex m_mutex = new Mutex(false, "328624A9-1CBB-4901-82D2-E10BD9EF0F0B");
        private Communication_TcpServer m_Server = null;
        private Print m_Print = new Print();
        private bool m_bHandPrint = false;

        private bool m_bTransfer = false;
        public FormMain()
        {
            InitializeComponent();
            Log_RichTextBoxEx.BindLogControl(richTextBox1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox_Total.Text = Configure.ReadConfig("SET", "TOTAL", "0");
            textBox_input.Enabled = false;

            if (!AccessHelper.CompactAccessDB())
            {
                MessageBox.Show("连接数据库失败，请检查是否有Data.mdb文件!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            AccessHelper.ConnectToDatabase();

            string strIPLocate = Configure.ReadConfig("SET", "IP", "127.0.0.1");
            int nPort = Configure.ReadConfig("SET", "PORT", 10086);
            m_Server = new Communication_TcpServer(strIPLocate, nPort);
            m_Server.ListenClients();
            Log_RichTextBoxEx.WriteMessage("服务器信息:" + strIPLocate + ":" + nPort.ToString());
            m_Server.acceptClientEvent += new AcceptClientEventHandler(m_Server_acceptClientEvent);
            m_Server.reciveClientEvent += new ReciveClientEventHandler(m_Server_reciveClientEvent);
            m_Server.closeClientEvent += new CloseClientEventHandler(m_Server_closeClientEvent);
        }
        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Configure.WriteConfig("SET", "TOTAL", textBox_Total.Text);
        }

        private void m_Server_acceptClientEvent(TcpClient client)
        {
            Log_RichTextBoxEx.WriteMessage("客户端连接:" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString() + ":" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Port.ToString());
        }
        private void m_Server_closeClientEvent(TcpClient client)
        {
            Log_RichTextBoxEx.WriteMessage("客户端关闭:" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString() + ":" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Port.ToString());
        }
        private void m_Server_reciveClientEvent(TcpClient client, string strRecvData)
        {
            Log_RichTextBoxEx.WriteMessage("接收客户端数据:" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString() + ":" + strRecvData);
            ThreadParase(strRecvData);
        }

        private void ThreadParase(string strReciveString)
        {
            string strError = "";
            try
            {
                m_mutex.WaitOne();
                string strRecive = strReciveString.Substring(0, strReciveString.Length - 1);
                Log_RichTextBoxEx.WriteMessage("截取后最终的打印数据为:" + strRecive);
                Log_RichTextBoxEx.WriteMessage("接收条码数据触发打印");
                PrintData(strRecive);
            }
            catch (Exception ex)
            {
                strError = "捕获到异常:" + ex.Message;
                Log_RichTextBoxEx.WriteMessage(strError, true);
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        private void PrintData(string strData)
        {
            try
            {
                string strDir = Configure.ReadConfig("SET", "MODEL_GRF", "");
                int nLoops = Configure.ReadConfig("SET", "COUNT", 1);
                string strName1 = Configure.ReadConfig("SET", "NAME1", "SN");
                string strName2 = Configure.ReadConfig("SET", "NAME2", "BR");
                bool bPrint = Convert.ToBoolean(Configure.ReadConfig("SET", "MarkNow", "True"));
                Log_RichTextBoxEx.WriteMessage("条码内容：" + strData);
                //界面显示
                this.Invoke((EventHandler)(delegate
                {
                    label_TXT.Text = strData;
                }));

                if (!bPrint)
                {
                    Log_RichTextBoxEx.WriteMessage("不执行打印流程,退出流程");
                    return;
                }

                Log_RichTextBoxEx.WriteMessage("开始执行打印流程");
                if (string.IsNullOrEmpty(strDir) || nLoops <= 0)
                {
                    Log_RichTextBoxEx.WriteMessage("请先设置打印机相关配置信息", true);
                    return;
                }

                string strPrintName = string.Format("{0}\\{1}.grf", strDir, strData.Length.ToString());
                if (!File.Exists(strPrintName))
                {
                    Log_RichTextBoxEx.WriteMessage("打印文件不存在:" + strPrintName, true);
                    return;
                }

                if (!m_Print.LoadGrfFile(strPrintName))
                {
                    Log_RichTextBoxEx.WriteMessage("加载打印模板失败:" + strPrintName, true);
                    return;
                }
                Log_RichTextBoxEx.WriteMessage("加载打印模板成功");

                if (!m_bHandPrint)
                {
                    Log_RichTextBoxEx.WriteMessage("自动模式检测防重");
                    int nResult = IsExist(strData);
                    if (nResult == -1)
                        return;
                    else if (nResult == 1)
                    {
                        if (MessageBox.Show("重复数据:" + strData + ",是否继续打印?", "检测到重码", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) != DialogResult.OK)
                        {
                            Log_RichTextBoxEx.WriteMessage("取消本次重复打印");
                            return;
                        }
                    }
                }
                else
                {
                    Log_RichTextBoxEx.WriteMessage("补码模式不检测防重");
                }

                if (!m_Print.ChangeTextByName(strName1, strData))
                {
                    Log_RichTextBoxEx.WriteMessage("打印模板替换内容失败", true);
                    return;
                }
                if (!m_Print.ChangeTextByName(strName2, strData))
                {
                    Log_RichTextBoxEx.WriteMessage("打印模板替换内容失败", true);
                    return;
                }
                Log_RichTextBoxEx.WriteMessage(string.Format("替换对象:{0}, 替换内容:{1}", strName1, strData));
                Log_RichTextBoxEx.WriteMessage(string.Format("替换对象:{0}, 替换内容:{1}", strName2, strData));

                for (int i = 0; i < nLoops; i++)
                {
                    m_Print.PrintDoc(false);
                }

                AddCount(nLoops);

                if (!SaveData(strData))
                {
                    Log_RichTextBoxEx.WriteMessage("保存数据失败:" + strData, true);
                }
                Log_RichTextBoxEx.WriteMessage("保存数据成功");

                Log_RichTextBoxEx.WriteMessage("+++++++++结束本次打印流程+++++++++");
            }
            catch (Exception ex)
            {
                Log_RichTextBoxEx.WriteMessage("PrintData 捕获到异常:" + ex.Message, true);
            }
            finally
            {
                this.Invoke((EventHandler)(delegate
                {
                    m_bHandPrint = false;
                    textBox_input.Text = "";
                    textBox_input.Focus();
                }));
            }
        }

        private void AddCount(int nAdd)
        {
            int nCurCount = 0;
            this.Invoke((EventHandler)(delegate
            {
                nCurCount = Convert.ToInt32(textBox_Total.Text);
                nCurCount += nAdd;
                textBox_Total.Text = nCurCount.ToString();
                Configure.WriteConfig("SET", "TOTAL", textBox_Total.Text);
            }));
        }
        private bool SaveData(string strBarcode)
        {
            string strTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return AccessHelper.InsertData(string.Format("insert into records values('{0}', '{1}')", strBarcode, Convert.ToDateTime(strTime)));
        }

        private int IsExist(string strBarcode)
        {
            string strSql = String.Format("select * from records where code ='{0}'", strBarcode);
            bool bExist = false;
            if (!AccessHelper.IsExist(strSql, ref bExist))
            {
                Log_RichTextBoxEx.WriteMessage("查询失败:" + strSql, true);
                return -1;
            }
            return bExist ? 1 : 0;
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            textBox_input.Focus();
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            this.Invoke((EventHandler)(delegate
            {
                textBox_Total.Text = "0";
                textBox_input.Text = "";
                textBox_input.Focus();
            }));
        }

        private void textBox_input_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (!string.IsNullOrEmpty(textBox_input.Text))
                {
                    Log_RichTextBoxEx.WriteMessage("回车响应补码打印");
                    m_bHandPrint = true;
                    PrintData(textBox_input.Text);
                }
            }
        }

        private void btn_Setting_Click(object sender, EventArgs e)
        {
            FormPWD pwd = new FormPWD();
            if (pwd.ShowDialog() == DialogResult.OK)
            {
                FormSetting formSet = new FormSetting();
                formSet.ShowDialog();
            }
        }

        private void btn_Lock_Click(object sender, EventArgs e)
        {
            if (!m_bTransfer)
            {
                FormPWD pwd = new FormPWD();
                if (pwd.ShowDialog() == DialogResult.OK)
                {
                    this.Invoke((EventHandler)(delegate
                    {
                        textBox_input.Enabled = true;
                        textBox_input.Text = "";
                        textBox_input.Focus();
                    }));

                    m_bTransfer = !m_bTransfer;
                }
            }
            else
            {
                textBox_input.Enabled = false;
                m_bTransfer = !m_bTransfer;
            }

        }

        private void textBox_Query_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (!string.IsNullOrEmpty(textBox_Query.Text))
                {
                    string strSql = String.Format("select * from records where code ='{0}'", textBox_Query.Text);
                    DataTable dt = null;
                    AccessHelper.ReadData(strSql, ref dt);
                    AccessHelper.ShowTableDataGridView(dataGridView, dt);
                }
            }
        }
    }
}
