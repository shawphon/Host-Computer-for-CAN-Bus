using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using DAL;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;

namespace UILayer
{
    public partial class BUsFlow : Form
    {
        #region 变量成员
        private int periodMessTimeFactor = 1;

        public int PeriodMessTimeFactor { get => periodMessTimeFactor; set => periodMessTimeFactor = value; }
        #endregion

        public BUsFlow()
        {
            InitializeComponent();
        }

        private void BUsFlow_Load(object sender, EventArgs e)
        {
            //textBox2
            textBox2.Text = Convert.ToString(periodMessTimeFactor);

            //Chart 绑定数据
            string sql = "select * from Tab_BusLoadFresh";
            DataTable dt = SQLHelper.ExecuteDataTable(sql);
            BindPointsXY(dt, chart1.Series[0].Points);

            timer1.Enabled = true;
            timer1.Interval = 20;
            timer1.Start();
        }

        /// <summary>
        /// Bind the chart with datable data
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="points"></param>
        private void BindPointsXY(DataTable dt, DataPointCollection points)
        {
            long[] x = new long[dt.Rows.Count];
            double[] y = new double[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                x[i] = Convert.ToInt64(dt.Rows[i][0]);
                y[i] = Convert.ToDouble(dt.Rows[i][1]);
            }
            points.DataBindXY(x, y);
        }

        /// <summary>
        /// update BusFlowForm
        /// update chart with data from Sql table Tab_BusLoadFresh
        /// update textBox2 with data from table Tab_BusLoadFresh (the latest data)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            string sql = "select * from Tab_BusLoadFresh";
            DataTable dt = SQLHelper.ExecuteDataTable(sql);
            if (dt.Rows.Count != 25)
            {
                dt.Clear();
                dt.Dispose();
                return;
            }
            //update chart
            BindPointsXY(dt, chart1.Series[0].Points);

            //update textBox2
            textBox1.Text = Convert.ToString(dt.Rows[24][1]);
            dt.Clear();
            dt.Dispose();
        }

        /// <summary>
        /// set the time used for transferring periodic Message ,time base is 200ms,here just set the factor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                textBox2.BackColor = Color.White;
                if (textBox2.Text != null)
                {
                    try
                    {
                        periodMessTimeFactor = Convert.ToInt32(textBox2.Text);
                        DAL.DataMonitor.PeriodMessTimeFactor = periodMessTimeFactor;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("周期时基应为正整数，并以十进制填写！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                textBox2.BackColor = Color.Yellow;    
            }

        }
    }
}
