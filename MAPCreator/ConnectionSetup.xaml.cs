using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MAPCreator
{
    /// <summary>
    /// Interaction logic for ConnectionSetup.xaml
    /// </summary>
    public partial class ConnectionSetup : Window
    {
        public ConnectionSetup()
        {
            InitializeComponent();
            ComPort1.ItemsSource = SerialPort.GetPortNames();
            ComPort2.ItemsSource = SerialPort.GetPortNames();
            ComPort3.ItemsSource = SerialPort.GetPortNames();
            ComPort4.ItemsSource = SerialPort.GetPortNames();
            
            if (TypeConnection.Text == "Serial Port")
            {
                gBoxSerialSetting.IsEnabled = true;
                gBoxServerSetting.IsEnabled = false;
            }

            if (TypeConnection.Text == "Modbus RTU Over TCP/IP")
            {
                gBoxServerSetting.IsEnabled = true;
                gBoxSerialSetting.IsEnabled = false;
            }

            int.TryParse(CountPorts.Text, out int temp);
            SPort1.IsEnabled = temp >= 1 ? true : false;
            SPort2.IsEnabled = temp >= 2 ? true : false;
            SPort3.IsEnabled = temp >= 3 ? true : false;
            SPort4.IsEnabled = temp >= 4 ? true : false;
            CPort1.IsEnabled = temp >= 1 ? true : false;
            CPort2.IsEnabled = temp >= 2 ? true : false;
            CPort3.IsEnabled = temp >= 3 ? true : false;
            CPort4.IsEnabled = temp >= 4 ? true : false;

        }

        private void CBoxTypeConnection_Selected(object sender, RoutedEventArgs e)
        {
            string temp = ((ComboBoxItem)sender).Content.ToString();
            if (temp == "Serial Port")
            {
                gBoxSerialSetting.IsEnabled = true;
                gBoxServerSetting.IsEnabled = false;
            }

            if (temp == "Modbus RTU Over TCP/IP")
            {
                gBoxServerSetting.IsEnabled = true;
                gBoxSerialSetting.IsEnabled = false;
            }
        }

        private void CBoxCountPorts_Selected(object sender, RoutedEventArgs e)
        {
            int.TryParse(((ComboBoxItem)sender).Content.ToString(), out int temp);

            SPort1.IsEnabled = temp >= 1 ? true : false;
            SPort2.IsEnabled = temp >= 2 ? true : false;
            SPort3.IsEnabled = temp >= 3 ? true : false;
            SPort4.IsEnabled = temp >= 4 ? true : false;
            CPort1.IsEnabled = temp >= 1 ? true : false;
            CPort2.IsEnabled = temp >= 2 ? true : false;
            CPort3.IsEnabled = temp >= 3 ? true : false;
            CPort4.IsEnabled = temp >= 4 ? true : false;
        }     
    }
}
