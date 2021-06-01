using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Collections;
using System.IO;
using System.Reflection;

using System.Net;

namespace GrabSmm
{
    public partial class form1 : Form
    {
        public form1()
        {
            InitializeComponent();
        }
        public string XdPath = Application.StartupPath.ToString();

        private void form1_Load(object sender, EventArgs e)
        {
            pager1.PageIndex = 1;
            QueryAllData();

            mytimer.Enabled = true;
            mytimer.Interval = 60000;//执行间隔时间,单位为毫秒;此时时间间隔为60秒
            mytimer.Start();   //定时器开始工作
        }
        /// <summary>
        /// 手动抓取【全部】
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnZQ_Click(object sender, EventArgs e)
        {
            btnZQ.Text = "抓取中...";

            btnZQ.Enabled = false;
            btxExe.Enabled = false;
            btnSearch.Enabled = false;
            ck_Auto.Enabled = false;
            textBox1.Enabled = false;

            var beginTime = "2020-10-01";
            var endtime = DateTime.Now.ToString("yyyy-MM-dd");
            try
            {
                //清空数据表
                Common.SqlData.SelectDataTable("", " truncate table smm_GrabData ");

                GrabSmm(beginTime, endtime);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抓取失败");
                Smm.AddLogToTXT(ex.Message);
            }
            

            btnZQ.Enabled = true;
            btxExe.Enabled = true;
            btnSearch.Enabled = true;
            ck_Auto.Enabled = true;
            textBox1.Enabled = true;
            btnZQ.Text = "全部抓取...";

            pager1.PageIndex = 1;
            //调用查询全部的方法
            QueryAllData();

            MessageBox.Show("抓取成功");
        }
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btxExe_Click(object sender, EventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", "\\") + "file\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            string sql = " select product_id,product_name,renew_date,high,low,average,change from smm_GrabData where 1=1 ";
            string txttitle = textBox1.Text.Trim();
            if (txttitle != "")
            {
                sql += " and product_name like '%" + txttitle + "%' ";
            }
            DataTable myTable = Common.SqlData.SelectDataTable("", sql);
            if (dt2csv(myTable, path, "有色金属网抓取信息", "编号,产品,日期,最高价,最低价,平均价,涨跌"))
            {
                MessageBox.Show("导出成功,文件位置:" + path);
            }
            else
            {
                MessageBox.Show("导出失败");
            }
        }
        /// <summary>
        /// 导出报表为Csv
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="strFilePath">物理路径</param>
        /// <param name="tableheader">表头</param>
        /// <param name="columname">字段标题,逗号分隔</param>
        public bool dt2csv(DataTable dt, string strFilePath, string tableheader, string columname)
        {
            try
            {
                string strBufferLine = "";
                StreamWriter strmWriterObj = new StreamWriter(strFilePath, false, System.Text.Encoding.UTF8);
                strmWriterObj.WriteLine(tableheader);
                strmWriterObj.WriteLine(columname);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    strBufferLine = "";
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (j > 0)
                            strBufferLine += ",";
                        strBufferLine += dt.Rows[i][j].ToString();
                    }
                    strmWriterObj.WriteLine(strBufferLine);
                }
                strmWriterObj.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// List转DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public DataTable ToDataTable<T>(IEnumerable<T> collection)
        {
            var props = typeof(T).GetProperties();
            var dt = new DataTable();
            dt.Columns.AddRange(props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray());
            if (collection.Count() > 0)
            {
                for (int i = 0; i < collection.Count(); i++)
                {
                    ArrayList tempList = new ArrayList();
                    foreach (PropertyInfo pi in props)
                    {
                        object obj = pi.GetValue(collection.ElementAt(i), null);
                        tempList.Add(obj);
                    }
                    object[] array = tempList.ToArray();
                    dt.LoadDataRow(array, true);
                }
            }
            return dt;
        }
        private void QueryAllData()
        {
            try
            {
                string txttitle = textBox1.Text.Trim();
                string sql = " select count(1) from smm_GrabData where 1=1 ";
                if (txttitle != "")
                {
                    sql += " and product_name like '%" + txttitle + "%' ";
                }
                pager1.PageSize = 20;
                int[] arr = cofig.config.CountStartEnd(pager1.PageIndex, pager1.PageSize);
                object o = Common.SqlData.ExecuteDataSql("", sql);
                int total = o == null ? 0 : (int)o;
                pager1.RecordCount = total;
                pager1.Page();

                string sql1 = "SELECT [product_id],[product_name],[renew_date],[high],[low],[average],[change] FROM(select *,ROW_NUMBER() over(order by renew_date desc) rows from smm_GrabData where product_name like '%{0}%') t where rows between " + arr[0] + " and " + arr[1] + " ";
                sql1 = string.Format(sql1, textBox1.Text);
                DataTable myTable = Common.SqlData.SelectDataTable("", sql1);
                dataGridView1.DataSource = myTable;

                //不允许添加行
                dataGridView1.AllowUserToAddRows = false;
                //背景为白色
                dataGridView1.BackgroundColor = Color.White;
                //只允许选中单行
                dataGridView1.MultiSelect = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询错误！" + ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            pager1.PageIndex = 1;
            QueryAllData();
        }
        /// <summary>
        /// 执行的定时方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mytimer_Tick(object sender, EventArgs e)
        {
            //如果当前时间是1点00分
            DateTime dt = DateTime.Now;
            string[] arrTime = cofig.config.GetTime(XdPath).Split(':');
            if (dt.Hour == Convert.ToInt32(arrTime[0]) && dt.Minute == Convert.ToInt32(arrTime[1]))
            {
                var beginTime = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");//抓取7天内的
                var endtime = DateTime.Now.ToString("yyyy-MM-dd");
                //执行定时抓取方法
                try
                {
                    GrabSmm(beginTime, endtime);
                }
                catch (Exception ex)
                {
                    Smm.AddLogToTXT(ex.Message);
                }
                //查询看看是否新增了数据
                QueryAllData();
            }
        }
        /// <summary>
        /// 窗口关闭 停止定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            mytimer.Enabled = false;
            mytimer.Stop();
        }

        /// <summary>
        /// 定时服务开启/关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ck_Auto_CheckedChanged(object sender, EventArgs e)
        {
            if (ck_Auto.Checked == true)
            {
                mytimer.Enabled = true;
                mytimer.Start();
            }
            else
            {
                mytimer.Enabled = false;
                mytimer.Stop();
            }
        }

        private void pager1_OnPageChanged(object sender, EventArgs e)
        {
            QueryAllData();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "链接" && e.RowIndex >= 0)
            {
                DataGridViewColumn column = dataGridView1.Columns[e.ColumnIndex];
                int row = this.dataGridView1.CurrentRow.Index;
                string projectPath = dataGridView1.Rows[row].Cells["链接"].Value.ToString();
                System.Diagnostics.Process.Start(projectPath);
            }
        }
        #region 抓取代码
        public static void GrabSmm(string beginTime, string endtime)
        {
            //A00铝：201102250398
            string product_ids = "201102250398,";
            //铜的编号
            string jsonUrl = Environment.CurrentDirectory.Replace("\\bin\\Debug", "\\") + "cofig\\copper.json";
            string readJson = File.ReadAllText((jsonUrl));
            Newtonsoft.Json.Linq.JObject obj =
                (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(readJson);

            var categorys = obj["category"];
            for (int i = 0; i < categorys.Count(); i++)
            {
                var products = categorys[i]["products"];
                for (int k = 0; k < products.Count(); k++)
                {
                    product_ids += products[k]["product_id"].ToString() + ",";
                }
            }
            product_ids = product_ids.TrimEnd(',');
            GetPriceInfo(product_ids,beginTime,endtime);
        }
        public static void GetPriceInfo(string product_ids,string beginTime,string endtime)
        {
            //登录获取token
            string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjZWxscGhvbmUiOiIxODQ4MjIyNzAxMCIsImNvbXBhbnlfaWQiOjAsImNvbXBhbnlfc3RhdHVzIjowLCJjcmVhdGVfYXQiOjE2MTc3MDExNzksImVtYWlsIjoiIiwiZW5fZW5kX3RpbWUiOjAsImVuX3JlZ2lzdGVyX3N0ZXAiOjEsImVuX3JlZ2lzdGVyX3RpbWUiOjAsImVuX3N0YXJ0X3RpbWUiOjAsImVuX3VzZXJfdHlwZSI6MCwiZW5kX3RpbWUiOjAsImlzX21haWwiOjAsImlzX3Bob25lIjowLCJsYW5ndWFnZSI6IiIsImx5X2VuZF90aW1lIjowLCJseV9zdGFydF90aW1lIjowLCJseV91c2VyX3R5cGUiOjAsInJlZ2lzdGVyX3RpbWUiOjE2MTYzOTQ4ODMsInN0YXJ0X3RpbWUiOjAsInVzZXJfaWQiOjE4OTIyMTYsInVzZXJfbGFuZ3VhZ2UiOiJjbiIsInVzZXJfbmFtZSI6IlNNTTE2MTYzOTQ4ODNwUiIsInVzZXJfdHlwZSI6MCwidXVpZF9zaGEyNTYiOiJkYzVkMjc3MDc2ZGJlZWMwN2Q3NjU2MGJkZGE2OGY5MDBlMmFlNGU3MDI2MGY5MWUwN2VkOTRhMTA2YTBiM2MwIiwienhfZW5kX3RpbWUiOjAsInp4X3N0YXJ0X3RpbWUiOjAsInp4X3VzZXJfdHlwZSI6MH0.m3Fov__WQLF271X22gwMs7jjLCbdWuUGzUEHS_cxoKk";

            //返回的json字符串
            var loginStr = Smm.sendHttpRequest("https://platform.smm.cn/usercenter//4.0/auth?user_name=18482227010&password=c8732f487472829362c112945a9d95c3&source=www&device_id=050651eb-e17a-5876-841c-51851a51f48b&source_link=https%3A%2F%2Fwww.smm.cn%2F&user_agent=chrome%2F89.0.4389.114", "");
            Newtonsoft.Json.Linq.JObject login =
                (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(loginStr);
            token = login["data"]["token"].ToString();

            //返回的json字符串
            var jsonStr = Smm.sendHttpRequest("https://platform.smm.cn/spotcenter/v31/settlement/history/prices?token=" + token + "&product_ids=" + product_ids + "&start_date=" + beginTime + "&end_date=" + endtime + "&is_not_spot =0", "");

            Newtonsoft.Json.Linq.JObject jobject =
                (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);


            var records = jobject["data"];
            for (int i = 0; i < records.Count(); i++)
            {
                var p = records[i]["prices"];
                for (int j = 0; j < p.Count(); j++)
                {
                    var strId =  p[j]["product_id"].ToString();
                    var strName = p[j]["product_name"].ToString();
                    var strDate = p[j]["renew_date"].ToString();
                    var strHigh = p[j]["high"].ToString();
                    var strLow = p[j]["low"].ToString();
                    var strAverage = p[j]["average"].ToString();
                    var strChange = p[j]["change"].ToString();

                    string strsql = "insert into smm_GrabData(product_id,product_name,renew_date,high,low,average,change) "
                        + " values('" + strId + "','" + strName + "','" + strDate + "','" + strHigh + "','" + strLow + "','" + strAverage + "','" + strChange + "')";
                    //判断是否存在重复记录
                    string strsql2 = "select * from smm_GrabData where product_id='" + strId + "' and renew_date='" + strDate + "'";
                    DataTable dt = Common.SqlData.SelectDataTable("", strsql2);
                    if (dt.Rows.Count == 0)
                    {
                        Common.SqlData.InsDelUpdData("", strsql);
                    }
                }
            }
        }
        #endregion
    }
}
