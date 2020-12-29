using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    /// <summary>
    /// TimeFilter.xaml 的交互逻辑
    /// </summary>
    public partial class TimeFilter : Window
    {
        public string status;
        public double value;
        public TimeFilter()
        {
            InitializeComponent();
            string path =System.Environment.CurrentDirectory+ConstDefintion.ConstPath_TimeFilterConfig;
            Read(path);
            if(status == ConstDefintion.ConstStr_TimeFilterStatusON)
            {
                rb_filterON.IsChecked = true;
            }
            else
            {
                rb_filterOFF.IsChecked = true;
            }
            tb_filterTime.Text = value.ToString();
        }

        public void Read(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            String line;
            //读取状态
            line = sr.ReadLine();
            status = line;
            line = sr.ReadLine();
            value = Convert.ToInt32(line);
            sr.Close();
        }
        public void Write()
        {
            FileStream fs = new FileStream(System.Environment.CurrentDirectory + ConstDefintion.ConstPath_TimeFilterConfig, FileMode.Create);
            //获得字节数组
            StreamWriter sw = new StreamWriter(fs);
            sw.Write($"{status}");
            sw.Write(Environment.NewLine);
            sw.Write($"{value}");
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }
        public void DeleteFile()
        {
            if (System.IO.File.Exists(System.Environment.CurrentDirectory + ConstDefintion.ConstPath_TimeFilterConfig))
            {
                File.Delete(System.Environment.CurrentDirectory + ConstDefintion.ConstPath_TimeFilterConfig);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (rb_filterON.IsChecked == true)
            {
                status = ConstDefintion.ConstStr_TimeFilterStatusON;
            }
            else
            {
                status = ConstDefintion.ConstStr_TimeFilterStatusOFF;
            }
            value = Convert.ToInt32(tb_filterTime.Text);
            DeleteFile();
            Write();
            this.DialogResult = true;
        }
    }
}
