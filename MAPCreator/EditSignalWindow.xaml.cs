using System;
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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class EditSignalWindow : Window
    {
        public EditSignalWindow()
        {
            InitializeComponent();
        }

        public delegate void AddSignal(Signal signal);
        public delegate void DelSignal(int index);
        public AddSignal addSignal;
        public DelSignal delSignal;
        public DevicePattern Pattern { get; set; }
        int selIndex;

        private void BtnAddSignal_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox1.Text != "" && comboBox2.Text != "" && comboBox3.Text != "" && comboBox4.Text != "")
            {
                Signal newSignal = new Signal();               
                newSignal.SignalName = comboBox2.Text;
                newSignal.DataType = comboBox3.Text;             
                try
                {
                    newSignal.FunctionCode = int.Parse(comboBox1.Text);
                    newSignal.RegisterAddress = int.Parse(comboBox4.Text);
                    newSignal.BitNumber = int.Parse(comboBox5.Text);      
                }
                catch { }
                addSignal.Invoke(newSignal);
            }
        }

        private void ListView_Selected(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == selIndex) listView.SelectedIndex = -1;
            else selIndex = listView.SelectedIndex;
        }

        private void BtnDelSignal_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex != -1) delSignal.Invoke(listView.SelectedIndex);
        }

        private void BtnGenSignal_Click(object sender, RoutedEventArgs e)
        {
            int count = int.Parse(countQF.Text);
            int n = 0;
            for (int i = 0; i < count * 2; i += 2)
            {
                Signal QF_vkl = new Signal() { FunctionCode = 3, SignalName = $"QF{int.Parse(numFirstQF.Text) + n} отключен", DataType = "BOOL", RegisterAddress = 51, BitNumber = i };
                addSignal.Invoke(QF_vkl);
                Signal QF_otkl = new Signal() { FunctionCode = 3, SignalName = $"QF{int.Parse(numFirstQF.Text) + n} включен", DataType = "BOOL", RegisterAddress = 51, BitNumber = i + 1 };
                addSignal.Invoke(QF_otkl);
                n++;
            }
        }

        private void BtnGenFromTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(numPowerInput.Text, out int powerInput))
            {
                foreach (Signal s in Pattern.DeviceSignal)
                {
                    if (s.SignalName.Contains("nQF"))
                    {
                        string editedSignal = s.SignalName.Replace("nQF", $"{powerInput}QF");
                        addSignal.Invoke(new Signal() {FunctionCode = s.FunctionCode, SignalName = editedSignal, DataType = s.DataType, RegisterAddress = s.RegisterAddress, BitNumber = s.BitNumber });
                    }
                    else addSignal.Invoke(s);
                }
            }
            else
            {
                foreach (Signal s in Pattern.DeviceSignal)
                {
                    addSignal.Invoke(s);
                }
            }
            
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
            while(listView.SelectedIndex != -1)
            {
                delSignal.Invoke(0);
            }
        }
    }
}