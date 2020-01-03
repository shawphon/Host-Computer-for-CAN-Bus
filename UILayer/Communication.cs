using System;
using System.Windows.Forms;
using CANDriverLayer;


namespace UILayer
{
    public partial class CommunicationForm : Form
    {

        #region 变量成员
        private UInt32[] arrDevType = new uint[8];
        private UInt32 devType = 4;
        private UInt32 devInd = 0;
        private UInt32 canInd = 0;
        private VCI_INIT_CONFIG pInitConfig = new VCI_INIT_CONFIG();
        private byte[] timing0 = new byte[14] { 0x00, 0x00, 0x80, 0x00, 0x80, 0x01, 0x81, 0x03, 0x04, 0x83, 0x09, 0x87, 0x18, 0x31 };
        private byte[] timing1 = new byte[14] { 0x14, 0x16, 0xB6, 0x1C, 0xFA, 0x1C, 0xFA, 0x1C, 0x1C, 0xFF, 0x1C, 0xFF, 0x1C, 0x1C };
        private int frameType = 0;
        #endregion

        #region 封装字段
        public uint DevType { get => devType; set => devType = value; }
        public uint DevInd { get => devInd; set => devInd = value; }
        public uint CanInd { get => canInd; set => canInd = value; }
        public byte[] Timing0 { get => timing0; set => timing0 = value; }
        public byte[] Timing1 { get => timing1; set => timing1 = value; }
        public uint[] ArrDevType { get => arrDevType; set => arrDevType = value; }
        public VCI_INIT_CONFIG PInitConfig { get => pInitConfig; set => pInitConfig = value; }
        public int FrameType { get => frameType; set => frameType = value; }
        #endregion


        public CommunicationForm(int countDevice)
        {
            InitializeComponent();
            cobDevInd.SelectedIndex = countDevice;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            //初始化组件 设备类型
            Int32 curIndex = 0;
            cobDevType.Items.Clear();
            curIndex = cobDevType.Items.Add("VCI_USBCAN-II");
            arrDevType[curIndex] = 4;

            //初始化组件 验收码 屏蔽码
            textCAN1AccCode.Text = "00000000";
            textCAN1AccMask.Text = "FFFFFFFF";
            textCAN2AccCode.Text = "00000000";
            textCAN2AccMask.Text = "FFFFFFFF";

            //初始化组件 波特率
            cobCAN1Baudrate.SelectedIndex = 5;
            cobCAN2Baudrate.SelectedIndex = 5;

            //初始化组件工作模式
            cobCAN1Mode.SelectedIndex = 0;
            cobCAN2Mode.SelectedIndex = 0;

            cobCAN1FrameFormat.SelectedIndex = 1;
            cobCAN2FrameFormat.SelectedIndex = 1;


            cobDevType.SelectedIndex = 0;
            cobDevType.MaxDropDownItems = cobDevType.Items.Count;
        }

        #region old
        //private void BtnOpenDev_Click(object sender, EventArgs e)
        //{
        //    //m_DevType = m_arrDevType[cobDevType.SelectedIndex];
        //    //m_DevInd = (UInt32)cobDevInd.SelectedIndex;
        //    //if (m_Open == 1) //关闭设备
        //    //{
        //    //    USBCANdll.VCI_CloseDevice(m_DevType, m_DevInd);
        //    //    MessageBox.Show("设备已关闭！", "Warning0002", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    //    dataGridView1.Rows.Clear();
        //    //    m_Open = 0;
        //    //    cobDevInd.Enabled = true;
        //    //    cobDevType.Enabled = true;
        //    //}
        //    //else//打开设备
        //    //{
        //    //    if (USBCANdll.VCI_OpenDevice(m_DevType, m_DevInd, 0) == 0)
        //    //    {
        //    //        MessageBox.Show("打开设备失败，请检查设备类型和设备索引号是否正确", "Error0001", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    //        return;
        //    //    }
        //    //    MessageBox.Show("设备已打开，请配置相应CAN通道参数！", "Infomation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    //    m_Open = 1;

        //    //    VCI_BOARD_INFO pInfo = new VCI_BOARD_INFO();
        //    //    USBCANdll.VCI_ReadBoardInfo(m_DevType, m_DevInd, ref pInfo);
        //    //    dataGridView1.Rows[0].Cells[0].Value = System.Text.Encoding.Default.GetString(pInfo.str_hw_Type);
        //    //    dataGridView1.Rows[0].Cells[1].Value = m_DevInd;
        //    //    dataGridView1.Rows[0].Cells[2].Value = "已连接";

        //    //    cobDevType.Enabled = false;
        //    //    cobDevInd.Enabled = false;
        //    //}
        //    //btnOpenDev.Text = m_Open == 1 ? "关闭设备" : "打开设备";
        //}

        //private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //    if (m_Open == 1)
        //    {
        //        USBCANdll.VCI_CloseDevice(m_DevType, m_DevInd);
        //    }
        //}

        //private void BtnStartCAN_Click(object sender, EventArgs e)
        //{
        //    VCI_INIT_CONFIG[] pInitConfig = new VCI_INIT_CONFIG[2];

        //    //初始化CAN1
        //    pInitConfig[0].AccCode = System.Convert.ToUInt32("0x" + textCAN1AccCode.Text, 16);
        //    pInitConfig[0].AccMask = System.Convert.ToUInt32("0x" + textCAN1AccMask.Text, 16);
        //    pInitConfig[0].Reserved = 0;
        //    pInitConfig[0].Filter = 0;
        //    pInitConfig[0].Mode = System.Convert.ToByte(cobCAN1Mode.SelectedIndex);
        //    pInitConfig[0].Timing0 = System.Convert.ToByte(m_Timing0[cobCAN1Baudrate.SelectedIndex]);
        //    pInitConfig[0].Timing1 = System.Convert.ToByte(m_Timing1[cobCAN1Baudrate.SelectedIndex]);

        //    //初始化CAN2
        //    pInitConfig[1].AccCode = System.Convert.ToUInt32("0x" + textCAN2AccCode.Text, 16);
        //    pInitConfig[1].AccMask = System.Convert.ToUInt32("0x" + textCAN2ACCMask.Text, 16);
        //    pInitConfig[1].Reserved = 0;
        //    pInitConfig[1].Filter = 0;
        //    pInitConfig[1].Mode = System.Convert.ToByte(cobCAN2Mode.SelectedIndex);
        //    pInitConfig[1].Timing0 = System.Convert.ToByte(m_Timing0[cobCAN2Baudrate.SelectedIndex]);
        //    pInitConfig[1].Timing1 = System.Convert.ToByte(m_Timing1[cobCAN2Baudrate.SelectedIndex]);

        //    if (m_Open == 0)
        //    {
        //        return;
        //    }

        //    if (chcBoxOpenAll.CheckState == 0)//只启动一路当前CAN
        //    {
        //        if (tabControl.SelectedTab == tabCAN1)
        //        {
        //            m_CANInd = (UInt32)tabControl.SelectedIndex;
        //        }
        //        else
        //        {
        //            m_CANInd = (UInt32)tabControl.SelectedIndex;
        //        }
        //        USBCANdll.VCI_InitCAN(m_DevType, m_DevInd, m_CANInd, ref pInitConfig[m_CANInd]);//初始化
        //        USBCANdll.VCI_StartCAN(m_DevType, m_DevInd, m_CANInd);//启动
        //    }
        //    else//启动所有CAN
        //    {
        //        m_CANInd = 0;
        //        USBCANdll.VCI_InitCAN(m_DevType, m_DevInd, m_CANInd, ref pInitConfig[m_CANInd]);//初始化CNA1
        //        USBCANdll.VCI_StartCAN(m_DevType, m_DevInd, m_CANInd);
        //        m_CANInd = 1;
        //        USBCANdll.VCI_InitCAN(m_DevType, m_DevInd, m_CANInd, ref pInitConfig[m_CANInd]);//初始化CAN2
        //        USBCANdll.VCI_StartCAN(m_DevType, m_DevInd, m_CANInd);
        //    }
        //    this.Visible = false;
        //}

        //private void BtnResetCAN_Click(object sender, EventArgs e)
        //{
        //    if (m_Open == 0)
        //    {
        //        return;
        //    }

        //    if (chcBoxOpenAll.CheckState == 0)
        //    {
        //        if (tabControl.SelectedTab == tabCAN1)
        //        {
        //            m_CANInd = 0;
        //        }
        //        else
        //        {
        //            m_CANInd = 1;
        //        }
        //        USBCANdll.VCI_ResetCAN(m_DevType, m_DevInd, m_CANInd);
        //        MessageBox.Show("重置CAN" + (m_CANInd + 1).ToString() + "通道成功", "Infomation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    else
        //    {
        //        m_CANInd = 0;
        //        USBCANdll.VCI_ResetCAN(m_DevType, m_DevInd, m_CANInd);
        //        m_CANInd = 1;
        //        USBCANdll.VCI_ResetCAN(m_DevType, m_DevInd, m_CANInd);
        //        MessageBox.Show("重置CAN1&CAN2通道成功", "Infomation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //    }
        //}
        #endregion


        private void BtnSaveConfig_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            DevType = arrDevType[cobDevType.SelectedIndex];
            devInd = Convert.ToUInt32(cobDevInd.Text);
            if (tabControl.SelectedTab == tabCAN1)
            {
                canInd = 0;
                pInitConfig.AccCode = Convert.ToUInt32("0x" + textCAN1AccCode.Text, 16);
                pInitConfig.AccMask = Convert.ToUInt32("0x" + textCAN1AccMask.Text, 16);
                pInitConfig.Mode = Convert.ToByte(cobCAN1Mode.SelectedIndex);
                pInitConfig.Timing0 = timing0[cobCAN1Baudrate.SelectedIndex];
                pInitConfig.Timing1 = timing1[cobCAN1Baudrate.SelectedIndex];
                frameType = cobCAN1FrameFormat.SelectedIndex;
            }
            else
            {
                canInd = 1;
                pInitConfig.AccCode = Convert.ToUInt32("0x" + textCAN1AccCode.Text, 16);
                pInitConfig.AccMask = Convert.ToUInt32("0x" + textCAN2AccMask.Text, 16);
                pInitConfig.Mode = Convert.ToByte(cobCAN2Mode.SelectedIndex);
                pInitConfig.Timing0 = timing0[cobCAN2Baudrate.SelectedIndex];
                pInitConfig.Timing1 = timing1[cobCAN2Baudrate.SelectedIndex];
                frameType = cobCAN2FrameFormat.SelectedIndex;
            }

        }

        private void CommunicationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DevType = arrDevType[cobDevType.SelectedIndex];
            devInd = Convert.ToUInt32(cobDevInd.Text);
            if (tabControl.SelectedTab == tabCAN1)
            {
                canInd = 0;
                pInitConfig.AccCode = Convert.ToUInt32("0x" + textCAN1AccCode.Text, 16);
                pInitConfig.AccMask = Convert.ToUInt32("0x" + textCAN1AccMask.Text, 16);
                pInitConfig.Mode = Convert.ToByte(cobCAN1Mode.SelectedIndex);
                pInitConfig.Timing0 = timing0[cobCAN1Baudrate.SelectedIndex];
                pInitConfig.Timing1 = timing1[cobCAN1Baudrate.SelectedIndex];
                frameType = cobCAN1FrameFormat.SelectedIndex;
            }
            else
            {
                canInd = 1;
                pInitConfig.AccCode = Convert.ToUInt32("0x" + textCAN1AccCode.Text, 16);
                pInitConfig.AccMask = Convert.ToUInt32("0x" + textCAN2AccMask.Text, 16);
                pInitConfig.Mode = Convert.ToByte(cobCAN2Mode.SelectedIndex);
                pInitConfig.Timing0 = timing0[cobCAN2Baudrate.SelectedIndex];
                pInitConfig.Timing1 = timing1[cobCAN2Baudrate.SelectedIndex];
                frameType = cobCAN2FrameFormat.SelectedIndex;
            }
        }

        public int GetBaudrate()
        {
            return Convert.ToInt32(cobCAN1Baudrate.Text.Split('k')[0]);
        }
    }
}
