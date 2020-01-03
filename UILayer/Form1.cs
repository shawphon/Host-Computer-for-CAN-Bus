using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CANDriverLayer;
using DAL;
using System.Threading;

namespace UILayer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region 变量成员
        //通信窗体， 负载率窗体， 设备驱动接口， 数据监控接口
        private CommunicationForm commuForm;
        private BUsFlow busFlowForm;
        private ICANDriver intfCANDriver1;
        private IDataMonitor intfDataMonitor;

        private UInt64 countFrame = 0;
        private int saveStatus = 0;

        public CommunicationForm CommuForm { get => commuForm; set => commuForm = value; }
        public ICANDriver IntfCANDriver1 { get => intfCANDriver1; set => intfCANDriver1 = value; }
        public BUsFlow BusFlowForm { get => busFlowForm; set => busFlowForm = value; }
        public IDataMonitor IntfDataMonitor { get => intfDataMonitor; set => intfDataMonitor = value; }
        #endregion


        #region 方法成员
        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.AutoGenerateColumns = false;

            //recThread = new Thread(Rec);
            this.timer1.Interval = 5000;
            timer1.Enabled = true;
            timer1.Start();
        }


        /// <summary>
        /// 配置设备通信参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (commuForm == null)
            {
                commuForm = new CommunicationForm(0);
            }
            commuForm.ShowDialog(this);
        }
        #endregion

        /// <summary>
        /// 打开设备，启动通道
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (commuForm == null)
            {
                MessageBox.Show("请先配置设备", "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (intfCANDriver1 == null)
            {
                intfCANDriver1 = new CANDriver(commuForm.DevType, commuForm.DevInd,
                    commuForm.CanInd, commuForm.PInitConfig, commuForm.FrameType);
            }
            if (intfDataMonitor == null)
            {
                intfDataMonitor = new DataMonitor(intfCANDriver1, commuForm.GetBaudrate());
            }
            //启动设备
            if (commuForm != null)
            {
                if (intfCANDriver1.GetDeviceStatus() == 0)
                {
                    if (intfCANDriver1.Open() == 1)
                    {
                        if (intfCANDriver1.Init() == 1)
                        {
                            if (intfCANDriver1.Start() != 1)
                            {
                                MessageBox.Show("启动失败", "Start", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                intfDataMonitor.OperateRecTimer(true);
                                MessageBox.Show("设备打开，通道启动成功", "Start", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("初始化失败", "Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("打开设备失败", "Init", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    if (intfCANDriver1.Start() != 1)
                    {
                        MessageBox.Show("启动失败", "Start", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        intfDataMonitor.OperateRecTimer(true);
                        MessageBox.Show("通道打开成功", "Start", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                
            }
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (intfCANDriver1 != null)
            {
                intfCANDriver1.Close();
                intfCANDriver1 = null;
                MessageBox.Show("通道已关闭，设备已禁用", "Stop", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (busFlowForm == null)
            {
                busFlowForm = new BUsFlow();
            }
            busFlowForm.ShowDialog(this);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (intfCANDriver1 != null & intfDataMonitor != null)
            {
                intfDataMonitor.OperateRecTimer(false);
                intfCANDriver1.Reset();
                MessageBox.Show("通道已关闭", "Stop", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            if (toolStripButton7.BackColor == Color.Transparent)
            {
                toolStripButton7.BackColor = Color.GreenYellow;
                saveStatus = 1;
                if (intfDataMonitor != null)
                {
                    intfDataMonitor.OperateBusLoadTabInsertTimer(true);
                    intfDataMonitor.OperateFrameInsertTimer(true);
                }
            }
            else
            {
                toolStripButton7.BackColor = Color.Transparent;
                saveStatus = 0;
                if (intfDataMonitor != null)
                {
                    intfDataMonitor.OperateBusLoadTabInsertTimer(false);
                    intfDataMonitor.OperateFrameInsertTimer(false);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string sql = "select * from can0data";
            DataTable dt = SQLHelper.ExecuteDataTable(sql);
            BindingSource bs = new BindingSource();
            bs.DataSource = dt;
            dataGridView1.DataSource = bs;

            dataGridView1.Columns[0].DataPropertyName = "id";
            dataGridView1.Columns[1].DataPropertyName = "timestamp";
            dataGridView1.Columns[2].DataPropertyName = "frameid";
            dataGridView1.Columns[3].DataPropertyName = "frametype";
            dataGridView1.Columns[4].DataPropertyName = "datalen";
            dataGridView1.Columns[5].DataPropertyName = "data";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }
    }
}
