using System;
using System.Runtime.InteropServices;
namespace CANDriverLayer
{

    public class CANDriver : ICANDriver
    {
        #region controlCAN.dll中的静态方法
        /// <summary>
        /// <param name="DeviceType"，设备类型></param>
        /// <param name="DeviceInd", 设备索引号，用于多相同设备标识></param>
        /// <param name="Reserved", 保留></param>
        /// <param name="CANInd"， CAN通道索引号
        /// <param name="pInitConfig", CAN初始化配置数据结构体></param>
        /// 返回值为1操作成功，0操作失败
        /// </summary>
        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);//打开设备

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);//关闭设备

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);//以pInitConfig参数初始化CAN设备

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);//读取板卡信息 pInfo

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 ReadErrInfo(UInt32 DeviceType, UInt32 DeviceInd, Int32 CANInd, IntPtr pErrInfo);//读取错误信息 pErrInfo

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 ReadCANStatus(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_STATUS pCANStatus);//读取某一已连的CAN设备的某一通道的状态，pCANStatus

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 GetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);//不用USBCANII设备

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);//不用USBCANII设备

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);//此函数从指定的设备 CAN 通道的接收缓冲区中读取数据,返回尚未被读取的帧数

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);//清除设备通道的缓冲区

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd); //此函数用以启动 CAN 卡的某一个 CAN 通道。有多个 CAN 通道时，需要多次调用。

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);//用于复位CAN，无需再初始化，和VCI_StartCAN联合使用

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]
        public static extern UInt32 Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, IntPtr pSend, UInt32 Len);//发送帧，注意此处的结构体是否为指针，可能会出错

        [DllImport("ECanVci.dll", CharSet = CharSet.Unicode)]//???
        public static extern UInt32 Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, IntPtr pReceive, UInt32 Len, Int32 WaitTime);//pReceive用来存储帧结构体VCI_CAN_OBJ数组的首指针； Len 本次接收的最大帧数；waitTime若为-1 则表示无超时，一直等待；返回值为实际读取到的帧数

        #endregion

        private static readonly object objLock = new object();

        private UInt32 canInd;//通道
        private UInt32 devInd;//设备索引
        private UInt32 devType = 4;//设备类型
        private Int32 isDeviceOpen = 0;//设备是否开启
        private Int32 operationStatus = 0;
        private UInt32[] isChannelOpen = new uint[2] { 0, 0 };//通道一是否开启
        private VCI_INIT_CONFIG pInitConfig;
        private VCI_ERR_INFO pErrInfo;
        private int frameType = 0;

        #region 封装字段
        public uint DevType { get => devType; set => devType = value; }
        public uint DevInd { get => devInd; set => devInd = value; }
        public uint CanInd { get => canInd; set => canInd = value; }
        public int IsDeviceOpen { get => isDeviceOpen; set => isDeviceOpen = value; }
        public uint[] IsChannelOpen { get => isChannelOpen; set => isChannelOpen = value; }
        public VCI_INIT_CONFIG PInitConfig { get => pInitConfig; set => pInitConfig = value; }
        public Int32 OperationStatus { get => operationStatus; set => operationStatus = value; }
        public VCI_ERR_INFO PErrInfo { get => pErrInfo; set => pErrInfo = value; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 用于默认的无参构造函数
        /// </summary>
        public CANDriver()
        {
            pInitConfig.AccCode = 0;
            pInitConfig.Filter = 1;
            pInitConfig.Timing0 = Convert.ToByte("0x00", 16);
            pInitConfig.Timing1 = Convert.ToByte("0x1C", 16);
            pInitConfig.Mode = 0;
        }

        /// <summary>
        /// 用于初始化设备的有参构造函数
        /// </summary>
        /// <param name="DevType"></param>
        /// <param name="DevInd"></param>
        /// <param name="CANInd"></param>
        /// <param name="pInitConfig"></param>
        public CANDriver(UInt32 DevType, UInt32 DevInd, UInt32 CANInd, VCI_INIT_CONFIG pInitConfig, int FrameType)
        {
            this.devType = DevType;
            this.devInd = DevInd;
            this.canInd = CANInd;
            this.pInitConfig = pInitConfig;
            this.frameType = FrameType;
        }

        public CANDriver(UInt32 DevType, UInt32 DevInd, UInt32 CANInd)
        {
            this.devType = DevType;
            this.devInd = DevInd;
            this.canInd = CANInd;
            pInitConfig.AccCode = 0;
            pInitConfig.Filter = 1;
            pInitConfig.Timing0 = Convert.ToByte("0x00", 16);
            pInitConfig.Timing1 = Convert.ToByte("0x1C", 16);
            pInitConfig.Mode = 0;
        }
        #endregion

        #region 接口实现成员
        public int Open()
        {
            if (isDeviceOpen == 1)
            {
                return isDeviceOpen;
            }
            isDeviceOpen = OpenDevice(this.devType, this.devInd, 0) == 1 ? 1 : 0;

            return isDeviceOpen;
        }

        public int Close()
        {
            if (isDeviceOpen == 1)
            {
                isDeviceOpen = CloseDevice(this.devType, this.devInd) == 1 ? 0 : 1;
            }
            return isDeviceOpen;
        }

        public int Init()
        {
            operationStatus = InitCAN(this.devType, this.devInd, this.canInd, ref this.pInitConfig) == 1 ? 1 : 0;
            return operationStatus;
        }

        public int Start()
        {
            //if (isDeviceOpen == 0)
            //{
            //    return 0;
            //}
            operationStatus = StartCAN(this.devType, this.devInd, this.CanInd) == 1 ? 1 : 0;
            return operationStatus;
        }

        public int Reset()
        {
            //if (isDeviceOpen == 0)
            //{
            //    return 0;
            //}
            operationStatus = ResetCAN(this.devType, this.devInd, this.canInd) == 1 ? 1 : 0;
            return operationStatus;
        }

        public int ReadError()
        {
            IntPtr ptErrInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_ERR_INFO)));
            int errCode = 0;
            UInt32 i = ReadErrInfo(devType, devInd, -1, ptErrInfo);
            pErrInfo = (VCI_ERR_INFO)Marshal.PtrToStructure(ptErrInfo, typeof(VCI_ERR_INFO));
            if ((pErrInfo.ErrCode &= 0x400) == 0x400)
            {
                errCode = 0x400;
            }
            if ((pErrInfo.ErrCode &= 0x100) == 0x100)
            {
                errCode = 0x100;
            }
            Marshal.DestroyStructure(ptErrInfo, typeof(VCI_ERR_INFO));
            Marshal.FreeHGlobal(ptErrInfo);

            return errCode;
        }

        /// <summary>
        /// 接收函数，传递参数为
        /// </summary>
        /// <param name="pRecFrameBufferFromCANSignal"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public uint Receive(ref VCI_CAN_OBJ[] pRecFrameBufferFromCANSignal)
        {
            lock (this)
            {
                UInt32 res = 0;
                UInt32 max = 100;
                res = GetReceiveNum(devType, devInd, canInd);
                if (res == 0)
                {
                    return 0;
                }
                IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * (int)max);
                res = res > 100 ? 100 : res;
                res = Receive(devType, devInd, canInd, pt, res, 100);
                for (int i = 0; i < res; i++)
                {
                    pRecFrameBufferFromCANSignal[i] = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((UInt64)pt + (UInt64)(i * Marshal.SizeOf(typeof(VCI_CAN_OBJ)))), typeof(VCI_CAN_OBJ));
                    #region MyRegion
                    //if (canInd == 0)
                    //{
                    //    byte[] data = pRecFrameBufferFromCANSignal[i].Data;
                    //    int count = 0;
                    //    foreach (byte ele in data)
                    //    {

                    //        if (ele == 1)
                    //        {
                    //            count++;
                    //        }
                    //    }
                    //    if (count == 8)
                    //    {
                    //        throw new Exception("通道一");
                    //    }
                    //}
                    //if (canInd == 1)
                    //{
                    //    byte[] data = pRecFrameBufferFromCANSignal[i].Data;
                    //    int count = 0;
                    //    foreach (byte ele in data)
                    //    {
                    //        if (ele == 255)
                    //        {
                    //            count++;
                    //        }
                    //    }
                    //    if (count == 8)
                    //    {
                    //        throw new Exception("通道二");
                    //    }
                    //}
                    //String str = "接收到数据: ";
                    //str += "  帧ID:0x" + System.Convert.ToString((Int32)pRecFrameBufferFromCANSignal[i].ID, 16);
                    //str += "  帧格式:";

                    //if (pRecFrameBufferFromCANSignal[i].RemoteFlag == 0)
                    //    str += "数据帧 ";

                    //if (pRecFrameBufferFromCANSignal[i].ExternFlag == 0)
                    //    str += "标准帧 ";
                    //else
                    //    str += "扩展帧 ";

                    //if (pRecFrameBufferFromCANSignal[i].RemoteFlag == 0)
                    //{
                    //    str += "数据: ";
                    //    byte len = (byte)(pRecFrameBufferFromCANSignal[i].DataLen % 9);
                    //    byte j = 0;
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[0], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[1], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[2], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[3], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[4], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[6], 16);
                    //    if (j++ < len)
                    //        str += " " + System.Convert.ToString(pRecFrameBufferFromCANSignal[i].Data[7], 16);
                    //    Console.WriteLine("通道1    " + str);
                    //}
                    #endregion
                }
                Marshal.DestroyStructure(pt, typeof(VCI_CAN_OBJ));
                Marshal.FreeHGlobal(pt);

                return res;

            }
        }

        /// <summary>
        /// 发送函数，pTxFrameBufferFromCANSignal 帧数据， len :本次需传输的帧长， 返回值: 发送成功的帧长
        /// </summary>
        /// <param name="pTxFrameBufferFromCANSignal"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public uint Transmit(ref VCI_CAN_OBJ pTxFrameBufferFromCANSignal, uint len)
        {
            //if (IsDeviceOpen == 0)
            //{
            //    return 0;
            //}
            pTxFrameBufferFromCANSignal.SendType = 0;                   //0正常发送， 1 单次发送，2自发自收，3单次自发自收
            pTxFrameBufferFromCANSignal.ExternFlag = Convert.ToByte(frameType);
            IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)));
            Marshal.StructureToPtr(pTxFrameBufferFromCANSignal, pt, true);
            uint flag = Transmit(devType, devInd, canInd, pt, len);
            Marshal.DestroyStructure(pt, typeof(VCI_CAN_OBJ));
            Marshal.FreeHGlobal(pt);
            return flag; //0时，没有发送成功数据
        }

        public void SetDeviceOpenStatus()
        {
            IsDeviceOpen = 1;
        }
        public int GetDeviceStatus()
        {
            return IsDeviceOpen;
        }
        #endregion

    }

    #region 结构体声明
    #region ControlCAN.dll

    /// <summary>
    /// 1.CAN接口卡信息的数据类型 VCI_BOARD_INFO
    /// hw_Version 硬件版本号
    /// fw_Version 固件版本号
    /// dr_Version 驱动版本号
    /// in_Version 接口版本号
    /// irq_Num 板卡所使用的的中断号
    /// can_Num 表示有几路CAN通道
    /// str_Serial_Num 此板卡的序列号
    /// str_hw_Type 硬件类型
    /// Reserved 系统保留 
    /// </summary>
    public struct VCI_BOARD_INFO
    {
        public UInt16 hw_Version;
        public UInt16 fw_Version;
        public UInt16 dr_Version;
        public UInt16 in_Version;
        public UInt16 irq_Num;
        public byte can_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Reserved;
    }

    /// <summary>
    /// 2.CAN帧结构体 VCI_CAN_OBJ
    /// ID 帧ID 数据格式为靠右对齐
    /// TimeStamp 设备接收到某一帧的时间标识，从CAN、卡上电开始
    ///TimeFlag 是否使用时间标识，1时TimeStamp有效
    ///SendType 发送发送帧方式：0 正常发送（发送失败会自动重发，最长时间为1.5~3s）
    ///                     1 单次发送（只发送一次，不自动重发）
    ///                     2 自发自收（自测试模式，用于测试CAN卡）
    ///                     3 单次自发自收（单次自测试模式）
    ///RemoteFlag 是否为远程帧 0为数据帧，1为远程帧
    ///ExternFlag 是否为扩展帧 0为标准帧，1为扩展帧
    ///DataLen 数据长度DLC（<=8）,即CAN帧Data有几个字节。约束了Data[8]的有效字节
    ///Data[8] CAN帧的数据区
    ///Reserved 系统保留
    /// </summary>
    public struct VCI_CAN_OBJ
    {
        public UInt32 ID;
        public UInt32 TimeStamp;
        public byte TimeFlag;
        public byte SendType;
        public byte RemoteFlag;
        public byte ExternFlag;
        public byte DataLen;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Reserved;
    }

    /// <summary>
    /// 3.CAN控制器状态信息 VCI_CAN_STATUS
    /// ErrInterrupt 中断记录，读操作会清除中断
    /// regMode CAN控制器模式寄存器值
    /// regStatus CAN控制器状态寄存器值
    /// regALCapture CAN控制器仲裁丢失寄存器值
    /// regECCapture CAN控制器错误寄存器值
    /// regEWLimit CAN控制器错误警告限制寄存器值，默认为96
    /// regRECounter CAN控制器接收错误寄存器值，为0-127，错误主动状态；128-254，错误被动状态；255总线关闭状态。
    /// regTECounter CAN控制器发送错误寄存器值，为0-127，错误主动状态；128-254，错误被动状态；255总线关闭状态。
    /// Reserved 系统保留
    /// </summary>
    public struct VCI_CAN_STATUS
    {
        public byte ErrInterrupt;
        public byte regMode;
        public byte regStatus;
        public byte regALCapture;
        public byte regECCapture;
        public byte regEWLimit;
        public byte regRECounter;
        public byte regTECounter;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved;
    }

    /// <summary>
    /// 4.定义错误信息
    /// ErrCode 错误码
    /// Passive_ErrData 被动错误的错误标识数据
    /// ArLost_ErrData 仲裁丢失错误的错误标识数据
    /// </summary>
    public struct VCI_ERR_INFO
    {
        public UInt32 ErrCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Passive_ErrData;
        public byte ArLost_ErrData;
    }

    /// <summary>
    /// 5.初始化CAN配置 VCI_INIT_CONFIG
    /// AccCode  帧过滤验收码
    /// AccMask 屏蔽码
    /// Reserved 保留
    /// Filter 滤波方式
    /// Timing0 波特率定时器0
    /// Timing1 波特率定时器1
    /// Mode 模式 =0 表示正常模式（相当于正常节点）
    ///           =1 表示只听模式（只接收，不影响总线）
    /// </summary>
    public struct VCI_INIT_CONFIG
    {
        public UInt32 AccCode;
        public UInt32 AccMask;
        public UInt32 Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;
    }

    ///// <summary>
    ///// CANET 通讯结构体 CHGDESIPANDPORT 此结构体用在 CANETE_UDP 与 CANET_TCP 
    ///// szpwd 更改目标ip所需的密码
    ///// szdesip 所要更改的目标ip
    ///// desport 所要更改的目标端口
    ///// blisten 所要更改的工作模式 0表示正常工作模式，1表示只听模式
    ///// </summary>
    //public struct CHGDESIPANDPORT
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    //    public byte[] szpwd;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    //    public byte[] szdesip;
    //    public Int32 desport;

    //    public void Init()//初始化
    //    {
    //        szpwd = new byte[10];
    //        szdesip = new byte[20];
    //    }
    //}
    #endregion

    #endregion
}
