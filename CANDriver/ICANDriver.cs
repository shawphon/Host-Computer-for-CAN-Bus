using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANDriverLayer
{
    public interface ICANDriver
    {
        int Open();
        #region ...
        /// <summary>
        /// 关闭设备，返回值为int，1表示成功，0失败
        /// </summary>
        /// <returns></returns>
        #endregion
        int Close();
        #region ...
        /// <summary>
        /// 初始化一路CAN通道函数，返回值为int，1表示成功,0失败
        /// </summary>
        /// <returns></returns>
        #endregion
        int Init();
        #region ...
        /// <summary>
        /// 启动一路CAN通道函数，返回值为int，1表示启动成功,0失败
        /// </summary>
        /// <returns></returns>
        #endregion
        int Start();
        #region ...
        /// <summary>
        /// 复位CAN通道通信函数，返回值为int，1表示复位成功,0失败
        /// </summary>
        /// <returns></returns>
        #endregion
        int Reset();
        #region ...
        /// <summary>
        /// 读板卡错误信息，返回值为int，标志板卡是否出错
        /// </summary>
        /// <returns></returns>
        #endregion
        int ReadError();

        void SetDeviceOpenStatus();
        #region ...
        /// <summary>
        /// 接收帧信息，传递参数为上层CANSignal中的信号信息，offset 为写入数组的偏移量，len为最大可写入的帧数，返回值为实际接收到的帧信息数
        /// </summary>
        /// <param name="pRecFrameBufferFromSignal"></param>
        /// <returns></returns>
        #endregion
        uint Receive(ref VCI_CAN_OBJ[] pRecFrameBufferFromSignal);
        #region ...
        /// <summary>
        /// 用于获得接收到的适合DBC解析帧信息结构，参数为上层Signal传递的帧结构数组，返回值为本次实际获得的帧数
        /// </summary>
        /// <param name="CANObjFromCANSignal"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        #endregion
        //UInt32 GetRecCANOBJ(ref VCI_CAN_OBJ[] CANObjFromCANSignal, int offset, UInt32 len);
        #region ...
        /// <summary>
        /// 帧发送函数，pTxFrameBuffer: 发送的结构体变量，len: 发送的帧数量，返回值: 为实际发送成功的帧结构体数组的长度
        /// </summary>
        /// <param name="pTxFrameBuffer"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        #endregion
        UInt32 Transmit(ref VCI_CAN_OBJ pTxFrameBufferFromCANSignal, UInt32 len);

        int GetDeviceStatus();
    }


}
