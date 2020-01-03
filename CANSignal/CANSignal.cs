using CANDriver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using DBCHandle = System.UInt32;


namespace CANSignal
{
    public class CANSignal : ICANSignal
    {
        #region 变量成员
        private static readonly object locker = new object();
        private Thread recThread1;
        private Thread recThread2;
        private Thread recThread3;

        private System.DateTime dateTime = new DateTime();
        private uint sendCount = 0;         //发送帧计数
        private int recCount = 0;           //接收帧计数
        private VCI_CAN_OBJ[] recFrame = new VCI_CAN_OBJ[100];     //接收帧更新值
        private DBCMessage[] messages = new DBCMessage[40];      //帧解析后的消息
        private uint countMessage;          //DBC消息总数
        private DBCHandle hDBC;             //DBC模块句柄
        private ICANDriver intfCANDriver;   //驱动层接口对象
        private CSVFileOperation savedCSV;
        private UInt32 countCSVRows = 0;        //计算CSV行数
        private readonly string strStableCSVName;
        DBC.OnSend sender;
        private Dictionary<uint, uint> dicMessageIDandPeriod = new Dictionary<uint, uint>();//消息ID和周期值字典
        private Dictionary<string, double> dicSignalNameandDefault = new Dictionary<string, double>();//消息ID和默认值
        private Dictionary<string, uint> dicSignalNameandMessageID = new Dictionary<string, DBCHandle>();//信号和消息ID
        //System.Timers.Timer recTimer;
        List<MillisecondTimer> listSendTimer = new List<MillisecondTimer>();
        #endregion

        #region 字段封装
        public VCI_CAN_OBJ[] RecFrame { get => recFrame; set => recFrame = value; }
        public DBCMessage[] Messages { get => messages; set => messages = value; }
        public uint CountMessage { get => countMessage; set => countMessage = value; }
        public uint HDBC { get => hDBC; set => hDBC = value; }
        public ICANDriver IntfCANDriver { get => intfCANDriver; set => intfCANDriver = value; }
        public Dictionary<uint, uint> DicMessageIDandPeriod { get => dicMessageIDandPeriod; set => dicMessageIDandPeriod = value; }
        public uint SendCount { get => sendCount; set => sendCount = value; }
        public int RecCount { get => recCount; set => recCount = value; }
        public DBC.OnSend Sender { get => sender; set => sender = value; }
        public CSVFileOperation SavedCSV { get => savedCSV; set => savedCSV = value; }
        public List<MillisecondTimer> ListSendTimer { get => listSendTimer; set => listSendTimer = value; }
        public uint CountCSVRows { get => countCSVRows; set => countCSVRows = value; }
        public Dictionary<string, double> DicSignalNameandDefault { get => dicSignalNameandDefault; set => dicSignalNameandDefault = value; }
        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public CANSignal(ref FileInfo fileInfo, ICANDriver iCANDriver, CSVFileOperation configCSV, CSVFileOperation SavedSignalCSV)
        {
            IntfCANDriver = iCANDriver;
            savedCSV = SavedSignalCSV;

            //savedCSV.DataTab.Columns.AddRange(new DataColumn[] { new DataColumn("TimeStamp", typeof(string)), new DataColumn("SignalName", typeof(string)), new DataColumn("value", typeof(double)) });
            strStableCSVName = savedCSV.FilePath;

            hDBC = DBC.DBC_Init();                          //初始化并启动DBC模块,并获取DBC中所有的消息及消息数
            bool flag0 = DBC.DBC_LoadFile(hDBC, ref fileInfo);

            if (flag0)
            {
                countMessage = DBC.GetDBCAllMessages(hDBC, ref messages);
            }

            Sender = new DBC.OnSend(OnSendFunc);           //设置DBC_Send的回调函数 OnSendFunc, 拟sender委托实现
            DBC.DBC_SetSender(hDBC, Sender, new IntPtr(10));

            DicMessageIDandPeriod.Clear();                //对CSV文件进行操作得到, 在这里，初始化消息ID和周期的字典，数据来源为配置文件,即将配置文件中的messageID和周期值添加到DicMessageIDandPeriod
            DataRow[] dataRows = configCSV.DataTab.Select("GroupName = 'Transmitted'");

            foreach (DataRow dr in dataRows)
            {
                DicMessageIDandPeriod[Convert.ToUInt32(dr["MessageID"])] = Convert.ToUInt32(dr["Period"]);
                dicSignalNameandMessageID[Convert.ToString(dr["SignalName"])] = Convert.ToUInt32(dr["MessageID"]);
                dicSignalNameandDefault[Convert.ToString(dr["SignalName"])] = Convert.ToDouble(dr["Default"]);
            }

            //接收消息的保存文件存储对象数据格式设置
            DataRow[] recDataRows = configCSV.DataTab.Select("GroupName = 'Received'");
            savedCSV.DataTab.Columns.Add(new DataColumn("TimeStamp", typeof(string)));
            foreach (DataRow dr in recDataRows)
            {
                savedCSV.DataTab.Columns.Add(new DataColumn(Convert.ToString(dr["SignalName"]), typeof(double)));
            }

            for (int i = 0; i < recFrame.Length; i++)       //初始化接收的帧的TimeFlag，使得时间戳信息有效。
            {
                recFrame[i].TimeFlag = 1;
            }

            //RecTimer = new System.Timers.Timer();
            //RecTimer.Enabled = false;
            //RecTimer.Elapsed += RecTimer_Elapsed;
            //recThread = new Thread(RecThread_Elapse);
            //recThread.Name = savedCSV.FilePath;
            //recThread.IsBackground = true;

            listSendTimer.Clear();
            if (DicMessageIDandPeriod != null)
            {
                foreach (uint key in DicMessageIDandPeriod.Keys) //messageID    period
                {
                    MillisecondTimer myTimer = new MillisecondTimer(key, DicMessageIDandPeriod[key]);
                    myTimer.Enabled = false;
                    myTimer.Tick += new EventHandler(SendTimer_Elapsed);
                    listSendTimer.Add(myTimer);
                }
            }

            ////消息信号初始化
            //foreach (uint id in DicMessageIDandPeriod.Keys)
            //{
            //    foreach (string key in dicSignalNameandMessageID.Keys) //key signalName  value messageID
            //    {
            //        if (id == dicSignalNameandMessageID[key])
            //        {
            //            SetSignalByNameFromApp(id, System.Text.Encoding.Default.GetBytes(key.Trim()), dicSignalNameandDefault[key]);
            //        }
            //    }
            //}//end 消息初始化

        }
        public CANSignal(ref FileInfo fileInfo, ICANDriver iCANDriver)
        {
            IntfCANDriver = iCANDriver;


            hDBC = DBC.DBC_Init();                          //初始化并启动DBC模块,并获取DBC中所有的消息及消息数
            bool flag0 = DBC.DBC_LoadFile(hDBC, ref fileInfo);

            if (flag0)
            {
                countMessage = DBC.GetDBCAllMessages(hDBC, ref messages);
            }

            Sender = new DBC.OnSend(OnSendFunc);           //设置DBC_Send的回调函数 OnSendFunc, 拟sender委托实现
            DBC.DBC_SetSender(hDBC, Sender, new IntPtr(10));
        }

        public CANSignal()
        {
        }

        #endregion

        #region 方法成员

        private void OnSendFunc(IntPtr pContext, IntPtr pObj)
        {
            //pContext上下文这里用不到, IntPtr pObj转换为VCI_CAN_OBJ
            VCI_CAN_OBJ canObj = (VCI_CAN_OBJ)Marshal.PtrToStructure(pObj, typeof(VCI_CAN_OBJ));
            IntfCANDriver.Transmit(ref canObj, 1);  //利用ICANDriver 进行发送 ICANDriver a = new CANDriver()
        }

        #region ...
        /// <summary>
        /// 在SetSignalByNameFromApp更新本地信号之后，可发送信息通过本函数,messageID，消息名由上层传递，timerPeriod由底层定时器根据时间周期发送消息
        /// </summary>
        /// <param name="messageID"></param>
        /// <param timerPeriod>
        /// <returns></returns>
        #endregion
        public void SendMessageByID(object messageID)
        {
            if (messageID == null)
            {
                return;
            }
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].nID == Convert.ToUInt32(messageID))
                {
                    IntPtr pt;
                    pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DBCMessage)));
                    Marshal.StructureToPtr(messages[i], pt, true);
                    if (DBC.DBC_Send(hDBC, pt))
                    {
                        sendCount++;    //线程中发送成功一次，计数值加1   
                    }
                    Marshal.DestroyStructure(pt, typeof(DBCMessage));
                    Marshal.FreeHGlobal(pt);
                    break;
                }
            }
        }
        public void RecFrame_UnpackToMessage(VCI_CAN_OBJ[] recFrame, uint res)
        {
            try
            {
                for (int i = 0; i < res; i++)   //调用DBC.dll更新到DBC中，此后DBC中与接收消息有关已更新
                {
                    DBC.DBC_OnReceive(HDBC, ref recFrame[i]); //对DBC里面的值进行更新
                }

                for (int i = 0; i < recFrame.Length; i++)
                {
                    for (int j = 0; j < messages.Length; j++)
                    {
                        if (messages[j].nID == recFrame[i].ID)
                        {
                            IntPtr pFrame = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)));
                            Marshal.StructureToPtr(recFrame[i], pFrame, true);
                            IntPtr pMessage = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DBCMessage)));
                            Marshal.StructureToPtr(messages[j], pMessage, true);

                            if (DBC.DBC_Analyse(hDBC, pFrame, pMessage)) //根据PFrame 获得接收的消息变量
                            {
                                messages[j] = (DBCMessage)Marshal.PtrToStructure(pMessage, typeof(DBCMessage));
                                dateTime = System.DateTime.Now;
                                DataRow dr = savedCSV.DataTab.NewRow();
                                for (int k = 0; k < messages[j].nSignalCount; k++)
                                {
                                    dr["TimeStamp"] = dateTime.Hour.ToString() + dateTime.Minute.ToString() + dateTime.Second.ToString() + dateTime.Millisecond.ToString();
                                    dr[System.Text.Encoding.Default.GetString(messages[j].vSignals[k].strName)] = messages[j].vSignals[k].nValue;
                                }
                                savedCSV.DataTab.Rows.Add(dr);
                                dr.Delete();
                                countCSVRows++;
                                if (countCSVRows > 500000)
                                {
                                    countCSVRows = 0;
                                    savedCSV.FilePath = String.Join("_", strStableCSVName.Split('.')[0], dateTime.Hour.ToString(), dateTime.Minute.ToString(), ".csv");
                                }
                            }
                            Marshal.DestroyStructure(pFrame, typeof(VCI_CAN_OBJ));
                            Marshal.FreeHGlobal(pFrame);
                            Marshal.DestroyStructure(pMessage, typeof(DBCMessage));
                            Marshal.FreeHGlobal(pMessage);
                            break;

                        }

                    }
                }
                //if (savedCSV.DataTab.Rows.Count != 0)
                //{
                //    savedCSV.SaveCSV();
                //    savedCSV.DataTab.Clear();
                //}
            }
            catch (Exception)
            {
            }

        }//end RecFrame_UnpackToMessage

        //#region ...
        ///// <summary>
        ///// 给本函数开线程接收帧并 解析 帧为Message及信号信息，返回值为实际接收到的帧数
        ///// </summary>
        ///// <param name="cANDriver"></param>
        ///// <returns></returns>  
        //#endregion
        //public void RecFrame_UnpackToMessage()
        //{
        //        try
        //        {
        //            UInt32 res = intfCANDriver.Receive(ref recFrame);   //先接收到帧           
        //            if (res == 0)
        //            {
        //                recCount += 0;
        //                return;
        //            }
        //            for (int i = 0; i < res; i++)   //调用DBC.dll更新到DBC中，此后DBC中与接收消息有关已更新
        //            {
        //                DBC.DBC_OnReceive(HDBC, ref recFrame[i]); //对DBC里面的值进行更新
        //            }

        //            for (int i = 0; i < recFrame.Length; i++)
        //            {
        //                for (int j = 0; j < messages.Length; j++)
        //                {
        //                    if (messages[j].nID == recFrame[i].ID)
        //                    {
        //                        IntPtr pFrame = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)));
        //                        Marshal.StructureToPtr(recFrame[i], pFrame, true);
        //                        IntPtr pMessage = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DBCMessage)));
        //                        Marshal.StructureToPtr(messages[j], pMessage, true);

        //                        if (DBC.DBC_Analyse(hDBC, pFrame, pMessage)) //根据PFrame 获得接收的消息变量
        //                        {
        //                            messages[j] = (DBCMessage)Marshal.PtrToStructure(pMessage, typeof(DBCMessage));
        //                            //dateTime = System.DateTime.Now;
        //                            //DataRow dr = savedCSV.DataTab.NewRow();
        //                            //for (int k = 0; k < messages[j].nSignalCount; k++)
        //                            //{
        //                            //    dr["TimeStamp"] = dateTime.Hour.ToString() + dateTime.Minute.ToString() + dateTime.Second.ToString() + dateTime.Millisecond.ToString();
        //                            //    dr[System.Text.Encoding.Default.GetString(messages[j].vSignals[k].strName)] = messages[j].vSignals[k].nValue;
        //                            //}
        //                            //savedCSV.DataTab.Rows.Add(dr);
        //                            //dr.Delete();
        //                            //countCSVRows++;
        //                            //if (countCSVRows > 500000)
        //                            //{
        //                            //    countCSVRows = 0;
        //                            //    savedCSV.FilePath = String.Join("_", strStableCSVName.Split('.')[0], dateTime.Hour.ToString(), dateTime.Minute.ToString(), ".csv");
        //                            //}
        //                        }
        //                        Marshal.DestroyStructure(pFrame, typeof(VCI_CAN_OBJ));
        //                        Marshal.FreeHGlobal(pFrame);
        //                        Marshal.DestroyStructure(pMessage, typeof(DBCMessage));
        //                        Marshal.FreeHGlobal(pMessage);
        //                        break;
        //                    }
        //                }

        //            }
        //            //if (savedCSV.DataTab.Rows.Count != 0)
        //            //{
        //            //    savedCSV.SaveCSV();
        //            //    savedCSV.DataTab.Clear();
        //            //}
        //        }
        //        catch (Exception)
        //        {
        //        }

        //}//end RecFrame_UnpackToMessage
        #endregion

        #region 实现接口方法成员

        public void StartTimer()
        {
            //RecTimer.Enabled = true;
            //RecTimer.Start();
            //recThread.Start();
            for (int i = 0; i < listSendTimer.Count; i++)
            {
                listSendTimer[i].Start();
            }
        }

        public void StopTimer()
        {
            //RecTimer.Stop();
            //RecTimer.Enabled = false;
            //recThread.Interrupt();
            for (int i = 0; i < listSendTimer.Count; i++)
            {
                //listSendTimer[i].Enabled = false;
                listSendTimer[i].Stop();
            }
        }

        private void SendTimer_Elapsed(object sender, EventArgs e)
        {
            //发送消息，首先根据ID进行信号赋值，其次发送消息
            SendMessageByID((sender as MillisecondTimer).MessageID);
        }

        //private void RecThread_Elapse()
        //{
        //    while (true)
        //    {
        //        lock (this)
        //        {
        //            RecFrame_UnpackToMessage();
        //            Thread.Sleep(500);
        //        }

        //    }

        //}

        #region ...
        /// <summary>
        /// 从APP中更新对应Signal的物理值
        /// </summary>
        /// <param name="MessageID"></param>
        /// <param name="byteArrySignalName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        #endregion
        public bool SetSignalByNameFromApp(uint MessageID, byte[] byteArrySignalName, double value)
        {
            if (messages == null)
            {
                return false;
            }
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].nID == MessageID)
                {
                    for (int j = 0; j < messages[i].vSignals.Length; j++)
                    {
                        if (System.Text.Encoding.Default.GetString(messages[i].vSignals[j].strName).Contains(System.Text.Encoding.Default.GetString(byteArrySignalName).Trim()))
                        {
                            messages[i].vSignals[j].nValue = value;
                            return true;    //更新成功
                        }
                    }
                }
            }
            return false;   //更新失败，或者未找到
        }

        #region ...
        /// <summary>
        /// 从本地获取对应Signal的值并返回成SignalInfo的变量
        /// </summary>
        /// <param name="MessageID"></param>
        /// <param name="strSignalName"></param>
        /// <returns></returns>
        #endregion
        public double GetSignalByNameToApp(uint MessageID, byte[] strSignalName)
        {
            string signalName = System.Text.Encoding.Default.GetString(strSignalName);
            foreach (DBCMessage message in messages)
            {
                if (message.nID == MessageID)
                {
                    foreach (DBCSignal signal in message.vSignals)
                    {
                        if (System.Text.Encoding.Default.GetString(signal.strName).Contains(signalName.Trim())) //找到signal在DBCMessage的位置
                        {
                            return signal.nValue; ;
                        }
                    }
                }
            }

            return -1;
        }

        public void Close()
        {
            this.intfCANDriver.Close();
        }

        public int GetMessageByIdToApp(uint MessageID, ref DBCMessage message)
        {
            foreach (DBCMessage ele in messages)
            {
                if (ele.nID == MessageID)
                {
                    message = ele;
                    return 1;
                }
            }
            return 0;
        }


        #endregion

    }//Class Signal

    public class MyTimer : System.Timers.Timer
    {
        private UInt32 messageID;

        public uint MessageID { get => messageID; set => messageID = value; }

        public MyTimer(UInt32 msgID, UInt32 period)
        {
            this.messageID = msgID;
            Interval = period;
        }
    }
    public sealed class MillisecondTimer : MyTimer, IComponent, IDisposable
    {
        #region 变量成员
        private static TimerCaps caps;
        private int interval;
        private bool isRunning;
        private int resolution;
        private TimerCallback timerCallback;
        private int timerID;
        #endregion

        #region 属性
        public new int Interval
        {
            get
            {
                return this.interval;
            }
            set
            {
                if ((value < caps.periodMin) || (value > caps.periodMax))
                {
                    throw new Exception("超出计时范围！");
                }
                this.interval = value;
            }
        }

        public bool IsRunning
        {
            get
            {
                return this.isRunning;
            }
        }

        public override ISite Site
        {
            set;
            get;
        }
        public int Resolution { get => resolution; set => resolution = value; }
        #endregion

        #region 事件
        public new event EventHandler Disposed;  // 这个事件实现了IComponet接口
        public event EventHandler Tick;
        #endregion

        #region 构造函数
        static MillisecondTimer()
        {
            timeGetDevCaps(ref caps, Marshal.SizeOf(caps));
        }

        public MillisecondTimer(UInt32 messageID, UInt32 period) : base(messageID, period)
        {
            this.interval = (int)period;    // 
            this.Resolution = caps.periodMin;  //

            this.isRunning = false;
            this.timerCallback = new TimerCallback(this.TimerEventCallback);
        }

        public MillisecondTimer(IContainer container, UInt32 messageID, UInt32 period) : base(messageID, period)
        {
            this.MessageID = messageID;
            this.interval = (int)period;
            container.Add(this);
        }

        #endregion

        #region 析构函数
        ~MillisecondTimer()
        {
            timeKillEvent(this.timerID);
        }
        #endregion

        #region 方法成员
        public new void Start()
        {
            if (!this.isRunning)
            {
                this.timerID = timeSetEvent(this.interval, this.Resolution, this.timerCallback, 0, 1); // 间隔性地运行

                if (this.timerID == 0)
                {
                    throw new Exception("无法启动计时器");
                }
                this.isRunning = true;
            }
        }

        public new void Stop()
        {
            if (this.isRunning)
            {
                timeKillEvent(this.timerID);
                this.isRunning = false;
            }
        }

        public new void Dispose()
        {
            timeKillEvent(this.timerID);
            GC.SuppressFinalize(this);
            EventHandler disposed = this.Disposed;
            if (disposed != null)
            {
                disposed(this, EventArgs.Empty);
            }
        }
        #endregion

        #region dll调用
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimerCallback callback, int user, int mode);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);
        #endregion

        private void TimerEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (this.Tick != null)
            {
                this.Tick(this, null);  // 引发事件
            }
        }

        private delegate void TimerCallback(int id, int msg, int user, int param1, int param2); // timeSetEvent所对应的回调函数的签名

        /// <summary>
        /// 定时器的分辨率（resolution）。单位是ms，毫秒？
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct TimerCaps
        {
            public int periodMin;
            public int periodMax;
        }

    }
}

