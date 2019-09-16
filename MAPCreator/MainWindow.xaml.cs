// Программа-конфигуратор карты адресного пространства
// Разработал: Чернышов Виктор
// Дата создания: 16.09.2019г
// Версия: 1.0

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;


namespace MAPCreator
{
    public class ShieldComparer : IComparer<ViewShield>
    {
        public int Compare(ViewShield x, ViewShield y)
        {
            if (x.SNum > y.SNum) return 1;
            else if (x.SNum < y.SNum) return -1;
            else return 0;
        }
    }

    [Serializable]
    public class Signal
    {
        public int? FunctionCode { get; set; }
        public string SignalName { get; set; }
        public string DataType { get; set; }
        public int? RegisterAddress { get; set; }
        public int? BitNumber { get; set; }
    }

    [Serializable]
    public class Header
    {
        public string ShieldName { get; set; }
        public int ShieldNumber { get; set; }
        public string DeviceType { get; set; }
        public int DeviceBaudRate { get; set; }
        public string DeviceParity { get; set; }
        public int DeviceAddress { get; set; }
    }

    public class DevicePattern
    {
        public List<Signal> DeviceSignal { get; set; }
        public string DeviceType { get; set; }
    }

    public class ViewDevice : Button
    {
        public ObservableCollection<Signal> Signal { get; set; }
        public Header Header {get; set; }

        public ViewDevice(Header header, RoutedEventHandler UpdateForm_Click)
        {
            this.Click += UpdateForm_Click;
            this.Header = header;
            this.Width = 80;
            this.Height = 40;
            this.Margin = new Thickness(0, 0, 0, 10);
            this.Content = new TextBlock() { Text = $"{header.DeviceType}\n(Slave {header.DeviceAddress})",TextAlignment = TextAlignment.Center };
            Signal = new ObservableCollection<Signal>();
        }
    }

    public class ViewShield : Button
    {
        public string SType { get; set; }
        public int SNum { get; set; }
        public StackPanel ShieldPanel { get; set; }

        public ViewShield(string sType, int sNum, RoutedEventHandler UpdateForm_Click)
        {
            this.SType = sType;
            this.SNum = sNum;
            this.ShieldPanel = new StackPanel();
            this.Content = ShieldPanel;
            this.VerticalContentAlignment = VerticalAlignment.Top;
            this.Background = Brushes.White;
            this.Height = 180;
            this.Width = 100;
            this.Cursor = Cursors.Hand;
            this.Margin = new Thickness(0, 5, 0, 0);
            this.Click += UpdateForm_Click;
            this.ShieldPanel.Children.Add(new Label()
            {
                Content = $"{sType} {sNum}",
                HorizontalContentAlignment = HorizontalAlignment.Center
            });
        }

        public void AddDevice(string dType, int dNum, int dBaud, string dParity, RoutedEventHandler UpdateForm_Click)
        {
            Header header = new Header()
            {
                ShieldName = SType,
                ShieldNumber =SNum,
                DeviceType = dType,
                DeviceBaudRate = dBaud,
                DeviceParity = dParity,
                DeviceAddress = dNum
            };
            this.ShieldPanel.Children.Add(new ViewDevice(header, UpdateForm_Click));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        ViewDevice SelectedDevice { get; set; }
        EditSignalWindow editSignalWindow;
        List<DevicePattern> Pattern { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            Pattern = new List<DevicePattern>();
            DataContext = Pattern;
            DirectoryInfo dir = new DirectoryInfo(@"Resources");
            FileInfo[] file = dir.GetFiles("*.csv");
            foreach (FileInfo f in file)
            {
                string deviceType = f.Name.Split('.')[0];
                string[] lines = File.ReadAllLines(@"Resources\" + f.Name, Encoding.GetEncoding(1251));
                List<Signal> signal = new List<Signal>();
                foreach (string s in lines)
                {
                    string[] properties = s.Split(';');
                    try
                    {
                        signal.Add(new Signal
                        {
                            FunctionCode = IntNullParse(properties[0]),
                            SignalName = properties[1],
                            DataType = properties[2],
                            RegisterAddress = IntNullParse(properties[3]),
                            BitNumber = IntNullParse(properties[4])
                        });
                    }
                    catch { }
                }
                Pattern.Add(new DevicePattern { DeviceType = f.Name.Split('.')[0], DeviceSignal = new List<Signal>(signal) });
            }
        }

        public int? IntNullParse(string s)
        {
            int? result = null;
            if (int.TryParse(s, out int value)) result = value;
            return result;
        }

        private void Object_GotFocus(object sender, RoutedEventArgs e)
        {
            btnAddDevice.IsEnabled = false;
            btnDelDevice.IsEnabled = false;
            btnEditDevice.IsEnabled = false;
        }

        private void UpdateForm_Click(object sender, RoutedEventArgs e)
        {     
            if (e.Source is ViewShield)
            {
                ViewShield selectedButton = (ViewShield)e.Source;
                cBoxSType.Text = selectedButton.SType;
                cBoxSNum.Text = selectedButton.SNum.ToString();
                btnAddDevice.IsEnabled = true;
                btnDelDevice.IsEnabled = true;
                btnEditDevice.IsEnabled = false;
            }
            else if (e.Source is ViewDevice)
            {
                this.SelectedDevice = (ViewDevice)e.Source;
                cBoxDType.Text = SelectedDevice.Header.DeviceType;
                cBoxDNum.Text = SelectedDevice.Header.DeviceAddress.ToString();
                cBoxDBaud.Text = SelectedDevice.Header.DeviceBaudRate.ToString();
                cBoxDParity.Text = SelectedDevice.Header.DeviceParity;
                btnEditDevice.IsEnabled = true;
                StackPanel selectedPanel = (StackPanel)SelectedDevice.Parent;
                ViewShield selectedShield = (ViewShield)selectedPanel.Parent;
                cBoxSType.Text = selectedShield.SType;
                cBoxSNum.Text = selectedShield.SNum.ToString();
            }
        }

        public void SortShield()
        {
            // Создаем два списка: один со шкафами УВН, другой с остальными
            List<ViewShield> UVNShield = new List<ViewShield>();
            List<ViewShield> otherShield = new List<ViewShield>();
            foreach (ViewShield s in myPanel.Children)
            {
                if (s.SType == "УВН") UVNShield.Add(s);
                else otherShield.Add(s);
            }
            // Очищаем список всех добавленных шкафов для формирования их нового расположения 
            myPanel.Children.Clear();
            // Сравниваем нумерацию шкафов и сортируем их
            ShieldComparer sc = new ShieldComparer();
            UVNShield.Sort(sc);
            otherShield.Sort(sc);
            // Расставляем все шкафы в один ряд. Сначала половина шкафов УВН, затем другие типы шкафов, а потом оставшиеся шкафы УВН
            int countUVNShield = UVNShield.Count;
            int countOtherShield = otherShield.Count;
            int countFirstUVN;
            int countLastUVN;
            if (countUVNShield % 2 == 0) countFirstUVN = countUVNShield / 2;
            else if (countUVNShield % 2 != 0 && countUVNShield == 1) countFirstUVN = countUVNShield;
            else countFirstUVN = countUVNShield / 2 + 1;
            countLastUVN = countUVNShield - countFirstUVN;
            for (int i = 0; i < countFirstUVN; i++)
            {
                myPanel.Children.Add(UVNShield[i]);
            }
            for (int i = 0; i < otherShield.Count; i++)
            {
                myPanel.Children.Add(otherShield[i]);
            }
            for (int i = countFirstUVN; i < countUVNShield; i++)
            {
                myPanel.Children.Add(UVNShield[i]);
            }
        }

        private void AddShield_Click(object sender, RoutedEventArgs e)
        {
            // Если введенные название шкафа и его номер не повторяются с уже существующими шкафами, то добавляем
            bool ok = int.TryParse(cBoxSNum.Text, out int sNum);
            foreach (ViewShield s in myPanel.Children)
            {
                if (s.SNum == sNum && s.SType == cBoxSType.Text) ok = false;
            }
            if (cBoxSType.Text=="") ok = false;
            if (ok) myPanel.Children.Add(new ViewShield(cBoxSType.Text, sNum, UpdateForm_Click));        
            SortShield();
        }

        private void DelShield_Click(object sender, RoutedEventArgs e)
        {
            bool ok = int.TryParse(cBoxSNum.Text, out int sNum);
            int pos = -1;
            foreach (ViewShield s in myPanel.Children)
            {
                pos++;
                if (s.SNum == sNum && s.SType==cBoxSType.Text)
                {
                    myPanel.Children.RemoveAt(pos);
                    break;
                }
            }
            SortShield();
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            int pos = -1;
            bool ok = int.TryParse(cBoxSNum.Text, out int sNum);
            if (!(int.TryParse(cBoxDNum.Text, out int dNum))) ok = false;
            if (!(int.TryParse(cBoxDBaud.Text, out int dBaud))) ok = false;
            if (cBoxDType.Text == "" || cBoxDParity.Text=="") ok = false;
            foreach (ViewShield s in myPanel.Children)
            {
                if (!ok) break;
                pos++;
                if (s.SNum == sNum && s.SType == cBoxSType.Text)
                {
                    s.AddDevice(cBoxDType.Text, dNum, dBaud, cBoxDParity.Text, UpdateForm_Click);
                    break;
                }
            }
        }

        private void DelDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int sNum = int.Parse(cBoxSNum.Text);
                int dNum = int.Parse(cBoxDNum.Text);
                foreach (ViewShield s in myPanel.Children)
                {
                    int pos = -1;
                    if (s.SNum == sNum)
                    {
                        foreach (object d in s.ShieldPanel.Children)
                        {
                            pos++;
                            if (d is ViewDevice)
                            {
                                if (((ViewDevice)d).Header.DeviceAddress == dNum)
                                {
                                    s.ShieldPanel.Children.RemoveAt(pos);
                                    btnEditDevice.IsEnabled = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void EditDevice_Click(object sender, RoutedEventArgs e)
        {
            string header = $"{SelectedDevice.Header.ShieldName} {SelectedDevice.Header.ShieldNumber}, " +
                $"{SelectedDevice.Header.DeviceType} (ID { SelectedDevice.Header.DeviceAddress}, " +
                $"{SelectedDevice.Header.DeviceBaudRate}, {SelectedDevice.Header.DeviceParity})";
            editSignalWindow = new EditSignalWindow
            {
                Title = header,
                DataContext = SelectedDevice.Signal,
                Name ="secondWindow"       
            };
            editSignalWindow.addSignal = AddSignal;
            editSignalWindow.delSignal = DelSignal;
            editSignalWindow.Pattern = Pattern.Find(p => p.DeviceType == SelectedDevice.Header.DeviceType);
            // Изменение видимости элементов в автогенерации
            if (SelectedDevice.Header.ShieldName != "ШОЛ" || SelectedDevice.Header.DeviceType != "МВ110")
            {
                editSignalWindow.numFirstQF.Visibility = Visibility.Hidden;
                editSignalWindow.numFirstQFlabelL.Visibility = Visibility.Hidden;
                editSignalWindow.numFirstQFlabelH.Visibility = Visibility.Hidden;
                editSignalWindow.countQF.Visibility = Visibility.Hidden;
                editSignalWindow.countQFlabelL.Visibility = Visibility.Hidden;
                editSignalWindow.countQFlabelH.Visibility = Visibility.Hidden;
                editSignalWindow.btnOK.Visibility = Visibility.Hidden;
            }
            if (((SelectedDevice.Header.ShieldName == "ШВА" && SelectedDevice.Header.DeviceType == "БМРЗ-АВ") ||
                (SelectedDevice.Header.ShieldName == "ШВЛ" && SelectedDevice.Header.DeviceType == "БМРЗ-ВВ") ||
                (SelectedDevice.Header.ShieldName == "ШВП" && SelectedDevice.Header.DeviceType == "БМРЗ-ВВ")) == false)
            {
                editSignalWindow.numPowerInput.Visibility = Visibility.Hidden;
                editSignalWindow.numPowerInputLabelH.Visibility = Visibility.Hidden;
                editSignalWindow.numPowerInputLabelL.Visibility = Visibility.Hidden;
            }

            editSignalWindow.ShowDialog();
        }

        private void AddSignal(Signal signal)
        {         
            SelectedDevice.Signal.Add(signal);
        }

        private void DelSignal(int index)
        {
            SelectedDevice.Signal.RemoveAt(index);
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            List<string> table = new List<string>();
            List<string> label = new List<string>();
            bool avrAV = false;
            foreach (ViewShield s in myPanel.Children)
            {
                foreach(object d in s.ShieldPanel.Children)
                {
                    if (d is ViewDevice)
                    {
                        table.Add( $"{((ViewDevice)d).Header.ShieldName} {((ViewDevice)d).Header.ShieldNumber};{((ViewDevice)d).Header.DeviceType};Modbus RTU, {((ViewDevice)d).Header.DeviceBaudRate}, {((ViewDevice)d).Header.DeviceParity};{((ViewDevice)d).Header.DeviceAddress};-;-;-;-;-");
                        foreach (Signal sg in ((ViewDevice)d).Signal)
                        {
                            table.Add($" ; ; ; ;{sg.FunctionCode} ;{sg.SignalName} ;{sg.DataType} ;{sg.RegisterAddress} ;{sg.BitNumber} ");
                            // Формируем таблицу бирок для БМЦС
                            if (((ViewDevice)d).Header.DeviceType == "БМЦС") label.Add(sg.SignalName);
                        }
                        // Добавляем в таблицу бирок сигналы о наличии аварийного ввода
                        if (((ViewDevice)d).Header.ShieldName == "ШВА") avrAV = true;
                    }
                } 
            }
            if (label.Count != 46)
            {
                label.Clear();
                for (int i = 0; i < 46; i++) label.Add("Резерв");
            }
            if (avrAV )
            {
                label.Insert(46, "Отключение ВВ при АВР АВ");
                label.Insert(47, "Включение ВВ при ВНР АВ");
            }
            else
            {
                label.Insert(46, "Резерв");
                label.Insert(47, "Резерв");
            }

            File.Copy(@"Resources\Report.xlt", "Отчёт №.xlt");
            File.WriteAllLines(@"data.xls", table.ToArray(), Encoding.UTF8);
            File.SetAttributes(@"data.xls", FileAttributes.ReadOnly);
            File.WriteAllLines(@"label.xls", label.ToArray(), Encoding.UTF8);
            File.SetAttributes(@"label.xls", FileAttributes.ReadOnly);            
            Process.Start(@"data.xls");
            Process.Start(@"label.xls");
            Process.Start(@"Отчёт №.xlt");
            File.SetAttributes(@"data.xls", FileAttributes.Normal);
            File.Delete(@"data.xls");
            File.SetAttributes(@"label.xls", FileAttributes.Normal);
            File.Delete(@"label.xls");
            File.Delete(@"Отчёт №.xlt");    
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Файл конфигурации (*.mapc)|*.mapc";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SerializationShield serializationShield = new SerializationShield();
                serializationShield.Read(dialog.FileName, myPanel, UpdateForm_Click);
                SortShield();
            }
            MessageBox.Show("Файл конфигурации успешно открыт!", "Открытие", MessageBoxButton.OK);
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Файл конфигурации (*.mapc)|*.mapc";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SerializationShield serializationShield = new SerializationShield();
                serializationShield.Write(dialog.FileName, myPanel);
            }
            MessageBox.Show("Файл конфигурации успешно сохранен!", "Сохранение", MessageBoxButton.OK);
        }
    }
}