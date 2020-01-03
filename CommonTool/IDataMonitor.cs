using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANDriverLayer;

namespace DAL
{
    /// <summary>
    /// 数据监测接口，
    /// 包含ICANDeriver 进行数据接收， 提供接收帧消息启动接口
    /// OperateRecThread(bool On)
    /// </summary>
    public interface IDataMonitor
    {

        /// <summary>
        /// 操作接收定时器开关
        /// </summary>
        /// <param name="timerOn"></param>
        void OperateRecTimer(bool timerOn);


        /// <summary>
        /// 获取接收总计帧数
        /// </summary>
        /// <returns></returns>
        UInt64 GetRecFrameCount();


        /// <summary>
        /// 负载利用率保存定时器开关
        /// </summary>
        /// <param name="timerOn"></param>
        /// <param name="saveOn"></param>
        void OperateBusLoadTabInsertTimer(bool timerOn);


        /// <summary>
        /// 帧信息保存定时器开关
        /// </summary>
        /// <param name="timerOn"></param>
        /// <param name="saveOn"></param>
        void OperateFrameInsertTimer(bool timerOn);


        VCI_CAN_OBJ[] GetCurrentFrameInfo();

    }
}
