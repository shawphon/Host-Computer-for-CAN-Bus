using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANDriverLayer;
using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Timers;
using System.Data;

namespace DAL
{
    public class DataMonitor : IDataMonitor
    {
        #region 变量成员
        private static double periodMessTimeFactor = 1;
        private ICANDriver intfCANDriver;
        private UInt64 countAllFrame;
        private VCI_CAN_OBJ[] frameStruct = new VCI_CAN_OBJ[100];
        private double busload = 0;
        private int baudrate;

        //定时器 recTimer 接收帧消息定时器， updateSQLTimer 更新负载率刷新表计时器 间隔都为40ms
        private System.Timers.Timer recTimer;
        private System.Timers.Timer updateSQLTimer;
        //insertBusLoadSQLTimer 插入负载率入数据库定时器， insertFrameSQLTimer 插入帧消息入数据库定时器 
        private System.Timers.Timer insertBusLoadSQLTimer;
        private System.Timers.Timer insertFrameSQLTimer;

        //lock 对象
        private object lockBusLoadFreshOBJ = new object();
        private object lockFrameOBJ = new object();

        private bool saveOn;
        private bool flagFrameNew = false;
        private uint countCurrentFrame40 = 0;
        #endregion


        #region 属性
        public ulong CountAllFrame { get => countAllFrame; set => countAllFrame = value; }
        public VCI_CAN_OBJ[] FrameStruct { get => frameStruct; set => frameStruct = value; }
        public static double PeriodMessTimeFactor { get => periodMessTimeFactor; set => periodMessTimeFactor = value; }
        #endregion


        #region 构造方法   
        public DataMonitor(ICANDriver intfCANDriver, int baudrate)
        {
            this.intfCANDriver = intfCANDriver;
            this.baudrate = baudrate;

            this.recTimer = new System.Timers.Timer();
            recTimer.Interval = 40;
            recTimer.Elapsed += RecTimer_Tick;
            recTimer.Enabled = true;

            this.updateSQLTimer = new System.Timers.Timer();
            updateSQLTimer.Interval = 40;
            updateSQLTimer.Elapsed += UpdateSQLTimer_Elapsed;
            updateSQLTimer.Enabled = true;

            this.insertFrameSQLTimer = new System.Timers.Timer();
            insertFrameSQLTimer.Interval = 40;
            insertFrameSQLTimer.Elapsed += InsertFrameSQLTimer_Elapsed;
            insertFrameSQLTimer.Enabled = true;

            this.insertBusLoadSQLTimer = new System.Timers.Timer();
            insertBusLoadSQLTimer.Interval = 40;
            insertBusLoadSQLTimer.Elapsed += InsertBusLoadSQLTimer_Elapsed;
            insertBusLoadSQLTimer.Enabled = true;

        }

        private void InsertBusLoadSQLTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            InsertBusLoadToSQL();
        }

        private void InsertFrameSQLTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (lockFrameOBJ)
            {
                InsertFrameToSQL();
            }
        }
        #endregion

        private void UpdateSQLTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (lockBusLoadFreshOBJ)
            {
                UpdateBusLoadToSQL();
            }
        }


        #region 方法成员
        /// <summary>
        /// 初始化负载利用率刷新表
        /// </summary>
        private void InitalBusLoadTab()
        {
            double[] temp = new double[25];
            //其次删除表中数据
            string sql = "truncate table Tab_BusLoadFresh";
            SQLHelper.ExecuteNonQuery(sql);

            for (int i = 0; i < temp.Length; i++)
            {
                sql = string.Format("INSERT INTO Tab_BusLoadFresh VALUES('{0}')",
                    temp[i]);
                SQLHelper.ExecuteNonQuery(sql);
            }
        }

        /// <summary>
        /// 接收定时器 40ms
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecTimer_Tick(object sender, EventArgs e)
        {
            lock (lockFrameOBJ)
            {
                countCurrentFrame40 = 0;
                if (intfCANDriver != null)
                {
                    VCI_CAN_OBJ[] recFrame = new VCI_CAN_OBJ[100];
                    countCurrentFrame40 = intfCANDriver.Receive(ref recFrame);
                    if (countCurrentFrame40 > 0)
                    {
                        flagFrameNew = true;        //标志当前从此获取的帧信息是否是最新的
                    }
                    frameStruct = recFrame;
                    countAllFrame += countCurrentFrame40;
                    CalBusLoad(countCurrentFrame40, frameStruct);
                }
            }
        }

        /// <summary>
        /// 计算总线负载率 recTimer_Tick中调用
        /// </summary>
        /// <param name="countFrame"></param>
        /// <param name="recFrame"></param>
        private double CalBusLoad(uint countFrame, VCI_CAN_OBJ[] recFrame)
        {
            if (countFrame == 0)
            {
                busload = 0;
            }
            else
            {
                double transBit = 0;
                for (int i = 0; i < countFrame; i++)
                {
                    if (recFrame[i].RemoteFlag == 0)
                    {
                        transBit = 47 + 8 * Convert.ToInt32(recFrame[i].DataLen) +
                            (34 + 8 * Convert.ToInt32(recFrame[i].DataLen)) / 5 + 1;
                    }
                }
                busload = Math.Round(transBit / (baudrate * recTimer.Interval * 10) *
                    periodMessTimeFactor, 2);//eg . baudrate 500  : 500K
            }
            return busload;
        }

        /// <summary>
        /// 插入当前负载利用率入数据库中
        /// </summary>
        private void InsertBusLoadToSQL()
        {
            string sql = string.Format("INSERT INTO Tab_BusLoadAll VALUES('{0}')",
                    busload);
            SQLHelper.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 插入当前帧数据信息入数据库中
        /// </summary>
        private void InsertFrameToSQL()
        {
            if (!flagFrameNew)
            {
                return;
            }
            if (countCurrentFrame40 != 0)
            {
                for (int i = 0; i < countCurrentFrame40; i++)
                {

                    string data = byteToHexStr(frameStruct[i].Data);
                    string timestamp = frameStruct[i].TimeStamp.ToString();
                    string sql = string.Format("INSERT INTO can0data VALUES('{0}','{1}','{2}','{3}', '{4}')",
                        timestamp, frameStruct[i].ID,  Convert.ToInt16(frameStruct[i].ExternFlag), 
                        frameStruct[i].DataLen, data);
                    System.Diagnostics.Debug.WriteLine(data);
                    SQLHelper.ExecuteNonQuery(sql);
                }
            }

            flagFrameNew = false;

        }

        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        /// <summary>
        /// 将当前计算得到的负载率更新到用于刷新数据库中
        /// </summary>
        private void UpdateBusLoadToSQL()
        {
            DataTable dt;
            //先读取
            string sql = "select * from Tab_BusLoadFresh";
            dt = SQLHelper.ExecuteDataTable(sql);
            double[] busLoadTemp = new double[dt.Rows.Count + 1];

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                busLoadTemp[i] = Convert.ToDouble(dt.Rows[i][1]);
            }

            //再更新
            //busLoadTemp[dt.Rows.Count] = busload;
            busLoadTemp[dt.Rows.Count] = busload;

            //其次删除表中数据
            sql = "truncate table Tab_BusLoadFresh";
            SQLHelper.ExecuteNonQuery(sql);

            //插入更新后的数据
            for (int i = 1; i < dt.Rows.Count + 1; i++)
            {
                sql = string.Format("INSERT INTO Tab_BusLoadFresh VALUES('{0}')",
                    busLoadTemp[i]);
                SQLHelper.ExecuteNonQuery(sql);
            }
            dt.Clear();
            dt.Dispose();
        }
        #endregion

        #region 接口实现
        /// <summary>
        /// 实现接口，操作定时器开关
        /// </summary>
        /// <param name="timerOn"></param>
        public void OperateRecTimer(bool timerOn)
        {
            if (timerOn)
            {
                recTimer.Start();
                updateSQLTimer.Start();
            }
            else
            {
                recTimer.Stop();
                updateSQLTimer.Stop();
                lock (lockBusLoadFreshOBJ)
                {
                    InitalBusLoadTab();
                }
            }
        }


        /// <summary>
        /// 实现接口，获得自接收以来，总共帧数
        /// </summary>
        /// <returns></returns>
        public ulong GetRecFrameCount()
        {
            return CountAllFrame;
        }

        public void OperateBusLoadTabInsertTimer(bool timerOn)
        {
            if (timerOn)
            {
                insertBusLoadSQLTimer.Start();
            }
            else
            {
                insertBusLoadSQLTimer.Stop();
            }
        }

        public void OperateFrameInsertTimer(bool timerOn)
        {
            if (timerOn)
            {
                insertFrameSQLTimer.Start();
            }
            else
            {
                insertFrameSQLTimer.Stop();
            }
        }

        public VCI_CAN_OBJ[] GetCurrentFrameInfo()
        {
            return FrameStruct;
        }
    }
    #endregion



    public sealed class MillisecondTimer : System.Timers.Timer, IComponent, IDisposable
    {
        #region 变量成员
        private static TimerCaps caps;
        private int interval;
        private bool isRunning;
        private int resolution;
        private TimerCallback timerCallback;
        private int timerID;
        private ISite site;
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
            get => site;
            set => site = value;
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
