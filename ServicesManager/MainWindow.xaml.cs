using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using PInvoke;

namespace ServicesManager
{
    public class TextBlockSize40:TextBlock
    {
        public TextBlockSize40():base()
        {
            FontSize = 34;
            Width = 500;
            Height = 45;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("Advapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        unsafe static extern bool GetServiceDisplayNameW(IntPtr hSCManager, string lpServiceName, char* lpDisplayName, ref int lpcchBuffer);
        public static T ByteToStuct<T>(byte[] DataBuff_) where T : struct
        {
            Type t = typeof(T);
            //得到结构体大小
            int size = Marshal.SizeOf(t);
            //数组长度小于结构体大小
            if (size > DataBuff_.Length)
            {
                return default(T);
            }

            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组cpoy到分配好的内存空间内
            Marshal.Copy(DataBuff_, 0, structPtr, size);
            //将内存空间转换为目标结构体
            T obj = (T)Marshal.PtrToStructure(structPtr, t);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            return obj;
        }
        readonly AdvApi32.SafeServiceHandle SvcHandle;
        async Task<AdvApi32.ENUM_SERVICE_STATUS[]?> GetSvcs()
        {
            return await Task.Run(() => 
            {
                int pb = 0;
                int lpsr = 0;
                int lprm = 0;
                byte[] buffer = new byte[1];
                AdvApi32.EnumServicesStatus(SvcHandle, AdvApi32.ServiceType.SERVICE_KERNEL_DRIVER, AdvApi32.ServiceStateQuery.SERVICE_STATE_ALL, buffer, ref pb, ref lpsr, ref lprm);
                if (pb == 0)
                {
                    string Code = Kernel32.GetLastError().GetMessage();
                    MessageBox.Show(Code);
                    return null;
                }
                buffer = new byte[pb];

                if (!AdvApi32.EnumServicesStatus(SvcHandle, AdvApi32.ServiceType.SERVICE_KERNEL_DRIVER, AdvApi32.ServiceStateQuery.SERVICE_STATE_ALL, buffer, ref pb, ref lpsr, ref lprm))
                {
                    string Code = Kernel32.GetLastError().GetMessage();
                    MessageBox.Show(Code);
                    return null;
                }
                List<AdvApi32.ENUM_SERVICE_STATUS> r = new List<AdvApi32.ENUM_SERVICE_STATUS>();
                int esize = Marshal.SizeOf(typeof(AdvApi32.ENUM_SERVICE_STATUS));
                for (int i = 0; i < lpsr; i++)
                {
                    AdvApi32.ENUM_SERVICE_STATUS Esa = ByteToStuct<AdvApi32.ENUM_SERVICE_STATUS>(buffer[(i * esize)..((i + 1) * esize)].ToArray());

                    //SvcList.Items.Add(new TextBlockSize40() { Text = Esa.lpServiceName });
                    r.Add(Esa);
                    //do some thing
                }

                return r.ToArray();
            });
        }

        public MainWindow()
        {
            InitializeComponent();
            SvcHandle = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if(SvcHandle.IsInvalid)
            {
                string Code = Kernel32.GetLastError().GetMessage();
                MessageBox.Show(Code);
                Close();
            }
            Loaded += async (e, v) =>
             {
                 FlushList();
             };
            
        }

        unsafe async private void SvcList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SvcName.Text = (SvcList.SelectedItem as TextBlock).Text.ToString();

                char* buf = (char*)Marshal.AllocHGlobal(512);
                int size = 512;
                GetServiceDisplayNameW(SvcHandle.DangerousGetHandle(), (SvcList.SelectedItem as TextBlock).Text.ToString(),  buf, ref size);
                DlpName.Text = new string(buf);

             

                Marshal.FreeHGlobal((IntPtr)buf);
            }
            catch (Exception)
            {

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SvcHandle.Close();
        }

        private void ScvScro_MouseWheel(object sender, MouseWheelEventArgs e)
        {
           
        }

        private void ScvScro_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = ScvScro;  //scrollview 为Scrollview的名字，在Xaml文件中定义。
            if (viewer == null)
                return;
            double num = Math.Abs((int)(e.Delta / 2));
            double offset = 0.0;
            if (e.Delta > 0)
            {
                offset = Math.Max((double)0.0, (double)(viewer.VerticalOffset - num));
            }
            else
            {
                offset = Math.Min(viewer.ScrollableHeight, viewer.VerticalOffset + num);
            }
            if (offset != viewer.VerticalOffset)
            {
                viewer.ScrollToVerticalOffset(offset);
                e.Handled = true;
            }
        }


        async void FlushList()
        {
            flush_button.IsEnabled = false;
            ScvScro.IsEnabled = false;
            SvcList.Items.Clear();
            await Task.Run(async () =>
            {
                var r = await GetSvcs();
                foreach (var i in r)
                {
                    
                    Dispatcher.Invoke(() => {
                        if (i.ServiceStatus.dwCurrentState == AdvApi32.ServiceState.SERVICE_RUNNING)
                            SvcList.Items.Add(new TextBlockSize40() { Text = i.lpServiceName, Foreground = new SolidColorBrush(Colors.Green) });
                        else if (i.ServiceStatus.dwCurrentState == AdvApi32.ServiceState.SERVICE_PAUSED)
                            SvcList.Items.Add(new TextBlockSize40() { Text = i.lpServiceName, Foreground = new SolidColorBrush(Colors.Yellow) });
                        else if (i.ServiceStatus.dwCurrentState == AdvApi32.ServiceState.SERVICE_STOPPED)
                            SvcList.Items.Add(new TextBlockSize40() { Text = i.lpServiceName, Foreground = new SolidColorBrush(Colors.Red) });
                        else
                            SvcList.Items.Add(new TextBlockSize40() { Text = i.lpServiceName, Foreground = new SolidColorBrush(Colors.Black) });
                    });
                }
            });
            ScvScro.IsEnabled = true;
            flush_button.IsEnabled = true;
        }

        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            FlushList();
        }

        private void uninstall_button_Click(object sender, RoutedEventArgs e)
        {
            if(UnloadNTDriver((SvcList.SelectedItem as TextBlock).Text.ToString()))
            {
                MessageBox.Show("Unstallled "+ (SvcList.SelectedItem as TextBlock).Text.ToString());
                FlushList();
            }
        }

        bool UnloadNTDriver(string szSvrName, bool stoponly = false)
        {
            bool bRet = false;
            AdvApi32.SafeServiceHandle? hServiceDDK = null;//NT驱动程序的服务句柄
            AdvApi32.SERVICE_STATUS SvrSta = new AdvApi32.SERVICE_STATUS();
            //打开SCM管理器

            //打开驱动所对应的服务
            hServiceDDK = AdvApi32.OpenService(SvcHandle, szSvrName, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);
            if (hServiceDDK.IsInvalid)
            {
                var err = Kernel32.GetLastError();
                MessageBox.Show(err.GetMessage());
                return bRet;
            }
            //停止驱动程序，如果停止失败，只有重新启动才能，再动态加载。  
            if (!AdvApi32.ControlService(hServiceDDK, AdvApi32.ServiceControl.SERVICE_CONTROL_STOP, ref SvrSta))
            {
                var err = Kernel32.GetLastError();
                if(stoponly)
                {
                    MessageBox.Show(err.GetMessage());
                    return bRet;
                }
            }
            //动态卸载驱动程序。  
            if (!stoponly)
                if (!AdvApi32.DeleteService(hServiceDDK))
                {
                    var err = Kernel32.GetLastError();
                    MessageBox.Show(err.GetMessage());
                    return bRet;
                }
            bRet = true;
            //离开前关闭打开的句柄
            hServiceDDK?.Close();
            return bRet;
        }

        private void stop_button_Click(object sender, RoutedEventArgs e)
        {
            if (UnloadNTDriver((SvcList.SelectedItem as TextBlock).Text.ToString(), true))
            {
                MessageBox.Show("Stopped " + (SvcList.SelectedItem as TextBlock).Text.ToString());
                FlushList();
            }
        }

        private void install_button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            
            ofd.DefaultExt = ".sys";
            ofd.Filter = "System File|*.sys";
            if (ofd.ShowDialog() == true)
            {
                //此处做你想做的事 ...=ofd.FileName; 
                if(LoadNTDriver(ofd.SafeFileName, ofd.FileName))
                {
                    MessageBox.Show("Installed " + ofd.SafeFileName);
                    FlushList();

                }

            }
        }

        bool LoadNTDriver(string lpszDriverName, string lpszDriverPath, bool StartupIncreate = false)
        {
            bool bRet = false;

            AdvApi32.SafeServiceHandle? hServiceDDK = null;//NT驱动程序的服务句柄


            //创建驱动所对应的服务
            hServiceDDK = AdvApi32.CreateService(SvcHandle,
                lpszDriverName, //驱动程序的在注册表中的名字  
                lpszDriverName, // 注册表驱动程序的 DisplayName 值  
                AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS, // 加载驱动程序的访问权限  
                AdvApi32.ServiceType.SERVICE_KERNEL_DRIVER,// 表示加载的服务是驱动程序  
                AdvApi32.ServiceStartType.SERVICE_DEMAND_START, // 注册表驱动程序的 Start 值  
                AdvApi32.ServiceErrorControl.SERVICE_ERROR_IGNORE, // 注册表驱动程序的 ErrorControl 值  
                lpszDriverPath, // 注册表驱动程序的 ImagePath 值  
                null,
                0,
                null,
                null,
                null);

            int dwRtn;
            //判断服务是否失败
            if (hServiceDDK.IsInvalid)
            {
                var err = Kernel32.GetLastError();
                MessageBox.Show(err.GetMessage());
                return bRet;
            }

            //开启此项服务
            if (StartupIncreate)
            {
                bRet = AdvApi32.StartService(hServiceDDK, 0, null);
                if (!bRet)
                {
                    var err = Kernel32.GetLastError();
                    MessageBox.Show(err.GetMessage());
                    return bRet;
                }
            }
            bRet = true;
            //离开前关闭句柄
            hServiceDDK.Close();
            
            return bRet;
        }

        private void start_button_Click(object sender, RoutedEventArgs e)
        {
            if (StartNTDriver((SvcList.SelectedItem as TextBlock).Text.ToString()))
            {
                MessageBox.Show("Started " + (SvcList.SelectedItem as TextBlock).Text.ToString());
                FlushList();
            }
        }

        bool StartNTDriver(string szSvrName)
        {
            bool bRet = false;
            AdvApi32.SafeServiceHandle? hServiceDDK = null;//NT驱动程序的服务句柄
            AdvApi32.SERVICE_STATUS SvrSta = new AdvApi32.SERVICE_STATUS();
            //打开SCM管理器

            //打开驱动所对应的服务
            hServiceDDK = AdvApi32.OpenService(SvcHandle, szSvrName, AdvApi32.ServiceAccess.SERVICE_START);
            if (hServiceDDK.IsInvalid)
            {
                var err = Kernel32.GetLastError();
                MessageBox.Show(err.GetMessage());
                return bRet;
            }
            //停止驱动程序，如果停止失败，只有重新启动才能，再动态加载。  
            bRet = AdvApi32.StartService(hServiceDDK, 0, null);
            if (!bRet)
            {
                var err = Kernel32.GetLastError();
                MessageBox.Show(err.GetMessage());
                return bRet;
            }
            bRet = true;
            //离开前关闭打开的句柄
            hServiceDDK?.Close();
            return bRet;
        }
    }
}
