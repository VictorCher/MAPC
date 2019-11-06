// Программа-конфигуратор карты адресного пространства
// Разработал: Чернышов Виктор
// Дата создания: 16.09.2019г

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
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
using System.Windows.Threading;

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

        /*public void ChangeBackground()
        {
            this.ClearValue(Control.BackgroundProperty); // Восстановление цвета по умолчанию
        }*/
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
            this.Margin = new Thickness(0, 0, 0, 15);
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
        int TempShieldNum { get; set; }
        int TempDeviceAdr { get; set; }
        EditSignalWindow editSignalWindow;
        ConnectionSetup connectionSetup;
        List<DevicePattern> Pattern { get; set; }

        public void LoadConfig()
        {
            InitializeComponent();
            NumberServerPort = new string[]{ "4001", "4002", "4003", "4004" };
            NumberComPort = new string[] { "COM1", "COM2", "COM3", "COM4" };
            // Определяем путь к папке где лежит исполняемый файл .exe для того, чтобы получить доступ
            // к папке с ресурсами (если программа вызывается не из той папки в которой лежит .exe)
            string globalPath = AppDomain.CurrentDomain.BaseDirectory;
            Pattern = new List<DevicePattern>();
            DataContext = Pattern;
            DirectoryInfo dir = new DirectoryInfo(globalPath+ @"\Resources");
            FileInfo[] file = dir.GetFiles("*.csv");
            foreach (FileInfo f in file)
            {
                string deviceType = f.Name.Split('.')[0];
                string[] lines = File.ReadAllLines(globalPath + @"\Resources\" + f.Name, Encoding.GetEncoding(1251));
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

        public MainWindow()
        {
            LoadConfig();
            string[] args = Environment.GetCommandLineArgs();

            if (args.Count()==2 )
            {
                if (!string.IsNullOrEmpty(args[1]) && File.Exists(args[1]))
                {
                    SerializationShield serializationShield = new SerializationShield();
                    serializationShield.Read(args[1], myPanel, UpdateForm_Click);
                    SortShield();
                    this.Title += " - " + args[1];
                }
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
                btnEditDeviceAdr.IsEnabled = false;
                TempShieldNum = selectedButton.SNum;
                btnEditShield.IsEnabled = true;
                EditShielNum_Label.Content = $"Изм. номер шкафа {selectedButton.SType} {selectedButton.SNum}?";
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
                TempDeviceAdr = SelectedDevice.Header.DeviceAddress;
                btnEditDeviceAdr.IsEnabled = true;
                EditDeviceAdr_Label.Content = $"Изм. адрес {SelectedDevice.Header.DeviceAddress} ({SelectedDevice.Header.DeviceType})?";
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
            int.TryParse(cBoxSNum.Text, out int sNum);
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

        private void EditShieldNum_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(cBoxSNum.Text, out int sNum);
            foreach (ViewShield s in myPanel.Children)
            {
                // Находим шкаф который хотим отредактировать
                if (s.SNum == TempShieldNum && s.SType == cBoxSType.Text)
                {
                    // Проверяем будет ли шкаф уникальным после редактирования 
                    bool free = true;
                    foreach (ViewShield search in myPanel.Children)
                    {
                        if (search.SNum == sNum && search.SType == cBoxSType.Text)
                        {
                            free = false;
                            break;
                        }
                    }
                    // Меняем номер шкафа и заголовки у устройств в этом шкафу
                    if (free == true) {
                        btnEditShield.IsEnabled = false;
                        EditShielNum_Label.Content = "";
                        s.SNum = sNum;
                        foreach (object d in s.ShieldPanel.Children)
                        {
                            if (d is Label)
                            {
                                ((Label)d).Content = $"{s.SType} {sNum}";

                            }
                            if (d is ViewDevice)
                            {
                                ((ViewDevice)d).Header.ShieldNumber = sNum;
                            }
                        }
                        break;
                    }
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

        private void EditDeviceAdr_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(cBoxSNum.Text, out int sNum);
            int.TryParse(cBoxDNum.Text, out int dNum);
            foreach (ViewShield s in myPanel.Children)
            {
                // Находим шкаф в котором редактируемое устройство
                if (s.SNum == sNum && s.SType == cBoxSType.Text)
                {                   
                    // Меняем номер шкафа и заголовки у устройств в этом шкафу    
                    
                    
                    // Находим устройство, которое хотим отредактировать
                    foreach (object d in s.ShieldPanel.Children)
                    {
                        if (d is ViewDevice)
                        {
                            if (((ViewDevice)d).Header.DeviceAddress == TempDeviceAdr)
                            {
                                btnEditDeviceAdr.IsEnabled = false;
                                EditDeviceAdr_Label.Content = "";
                                ((ViewDevice)d).Header.DeviceAddress = dNum;
                                ((ViewDevice)d).Content = new TextBlock()
                                {
                                    Text = $"{((ViewDevice)d).Header.DeviceType}\n(Slave {((ViewDevice)d).Header.DeviceAddress})",
                                    TextAlignment = TextAlignment.Center
                                };
                            }
                            //this.Content = new TextBlock() { Text = $"{header.DeviceType}\n(Slave {header.DeviceAddress})", TextAlignment = TextAlignment.Center };
                        }
                    }
                    break;
                }
            }
            SortShield();
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

        private async void Report_Click(object sender, RoutedEventArgs e)
        {
            List<string> table = new List<string>();
            List<string> label = new List<string>();
            bool avrAV = false;

            progressBar.IsIndeterminate = true;
            progressBar.Visibility = Visibility.Visible;
            statusBar.Text = "Подготовка данных...";
            await Task.Delay(100);

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

            statusBar.Text = "Создание временных файлов...";
            await Task.Delay(100);

            File.Copy(@"Resources\Report.xlt", "Отчёт №.xlt");
            File.WriteAllLines(@"data.xls", table.ToArray(), Encoding.UTF8);
            File.SetAttributes(@"data.xls", FileAttributes.ReadOnly);
            File.WriteAllLines(@"label.xls", label.ToArray(), Encoding.UTF8);
            File.SetAttributes(@"label.xls", FileAttributes.ReadOnly);

            statusBar.Text = "Открытие файла отчета...";
            await Task.Delay(100);

            Process.Start(@"data.xls");
            Process.Start(@"label.xls");
            Process.Start(@"Отчёт №.xlt");
            File.SetAttributes(@"data.xls", FileAttributes.Normal);

            statusBar.Text = "Удаление временных файлов...";
            await Task.Delay(100);

            File.Delete(@"data.xls");
            File.SetAttributes(@"label.xls", FileAttributes.Normal);
            File.Delete(@"label.xls");
            File.Delete(@"Отчёт №.xlt");

            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Hidden;
            statusBar.Text = "Готово";
            MessageBox.Show("Файл отчета успешно создан!", "Создание отчета", MessageBoxButton.OK);
            statusBar.Text = "";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Файл конфигурации (*.mapc)|*.mapc";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();

            progressBar.IsIndeterminate = true;
            progressBar.Visibility = Visibility.Visible;
            statusBar.Text = "Выполняется открытие файла...";
            await Task.Delay(100);

            if (result == true)
            {
                SerializationShield serializationShield = new SerializationShield();
                serializationShield.Read(dialog.FileName, myPanel, UpdateForm_Click);
                SortShield();
            }
            this.Title = "Map Creator - " + dialog.FileName;

            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Hidden;
            statusBar.Text = "Готово";
            MessageBox.Show("Файл конфигурации успешно открыт!", "Открытие", MessageBoxButton.OK);
            statusBar.Text = "";
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Файл конфигурации (*.mapc)|*.mapc";
            dialog.RestoreDirectory = true;     
            bool? result = dialog.ShowDialog();

            progressBar.IsIndeterminate = true;
            progressBar.Visibility = Visibility.Visible;
            statusBar.Text = "Выполняется сохранение файла...";
            await Task.Delay(100);

            if (result == true)
            {
                SerializationShield serializationShield = new SerializationShield();
                serializationShield.Write(dialog.FileName, myPanel);
                this.Title = "Map Creator - " + dialog.FileName;
            }

            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Hidden;
            statusBar.Text = "Готово";
            MessageBox.Show("Файл конфигурации успешно сохранен!", "Сохранение", MessageBoxButton.OK);
            statusBar.Text = "";
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Извините! Справка пока не доступна", "Справка", MessageBoxButton.OK);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.ToString();
            string verion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string f = System.Reflection.Assembly.GetExecutingAssembly().GetName().ToString();
            MessageBox.Show($"Название продукта: {name}{'\n'}Версия продукта: {verion}{'\n'}© Чернышов В.В., 2019", "О программе", MessageBoxButton.OK);
        }

        public string[] NumberComPort { get; set; }
        public string ComParity { get; set; }
        public string ComBaudRate { get; set; }
        public string TypeConnection { get; set; }
        public string CountPorts { get; set; }
        public string ServerIP { get; set; }
        public string[] NumberServerPort { get; set; }
        private void ConnectionSetup_Click(object sender, RoutedEventArgs e)
        {
            connectionSetup = new ConnectionSetup();
            connectionSetup.ComPort1.Text = NumberComPort[0];
            connectionSetup.ComPort2.Text = NumberComPort[1];
            connectionSetup.ComPort3.Text = NumberComPort[2];
            connectionSetup.ComPort4.Text = NumberComPort[3];
            connectionSetup.TypeConnection.Text = TypeConnection;
            connectionSetup.CountPorts.Text = CountPorts;
            connectionSetup.ServerPort1.Text = NumberServerPort[0];
            connectionSetup.ServerPort2.Text = NumberServerPort[1];
            connectionSetup.ServerPort3.Text = NumberServerPort[2];
            connectionSetup.ServerPort4.Text = NumberServerPort[3];
            connectionSetup.ServerIP.Text = ServerIP;

            connectionSetup.ShowDialog();

            NumberComPort[0] = connectionSetup.ComPort1.Text;
            NumberComPort[1] = connectionSetup.ComPort2.Text;
            NumberComPort[2] = connectionSetup.ComPort3.Text;
            NumberComPort[3] = connectionSetup.ComPort4.Text;
            TypeConnection = connectionSetup.TypeConnection.Text;
            CountPorts = connectionSetup.CountPorts.Text;
            ServerIP = connectionSetup.ServerIP.Text;
            NumberServerPort[0] = connectionSetup.ServerPort1.Text;
            NumberServerPort[1] = connectionSetup.ServerPort2.Text;
            NumberServerPort[2] = connectionSetup.ServerPort3.Text;
            NumberServerPort[3] = connectionSetup.ServerPort4.Text;
        }

        SerialPort port;
        private async void ModbusTest_Click(object sender, RoutedEventArgs e)
        {
            // Читаем конфигурацию для опрашиваемых устройств
            try
            {
                progressBar.Visibility = Visibility.Visible;
                //statusBar.Text = "Выполняется тестирование...";
                foreach (ViewShield s in myPanel.Children) // Перебираем все шкафы
                {
                    foreach (object d in s.ShieldPanel.Children) // Перебираем все устройства
                    {
                        if (d is ViewDevice)
                        {
                            byte[] modbusTransmit = Modbus.Request((byte)((ViewDevice)d).Header.DeviceAddress, 3, 0, 2); // Формируем modbus-запрос
                                                                                                                         // Если используется Ethernet
                            if (TypeConnection == "Modbus RTU Over TCP/IP")
                            {
                                for (int i = 0; i < int.Parse(CountPorts); i++)
                                {
                                    byte modbusReceive = Modbus.Response(Modbus.Exchange(ServerIP, int.Parse(NumberServerPort[i]), modbusTransmit)); // Принимаем modbus-ответ
                                    bool testOK = modbusReceive == (byte)((ViewDevice)d).Header.DeviceAddress ? true : false;
                                    if (testOK)
                                    {
                                        ((ViewDevice)d).Background = Brushes.Green;
                                        break;
                                    }
                                    else ((ViewDevice)d).Background = Brushes.Red;
                                }
                            }
                            else if (TypeConnection == "Serial Port")
                            {
                                for (int i = 0; i < int.Parse(CountPorts); i++)
                                {
                                    int dataBits = 8;
                                    string stopBits = "1";
                                    try
                                    {
                                        port = new SerialPort();
                                        port.PortName = NumberComPort[i];
                                        port.BaudRate = ((ViewDevice)d).Header.DeviceBaudRate;
                                        port.Parity = (Parity)Enum.Parse(typeof(Parity), ((ViewDevice)d).Header.DeviceParity);
                                        port.DataBits = dataBits;
                                        port.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);
                                        port.ReadTimeout = 100;
                                    }
                                    catch
                                    {
                                        MessageBox.Show("Ошибка соединения\nПожалуй проверьте настройки сети и попробуйте еще раз!");
                                    }
                                    try
                                    {
                                        port.Open();
                                    }
                                    catch (Exception)
                                    {
                                        MessageBox.Show("Невозможно открыть порт");
                                        return;
                                    }
                                    // Запись сообщения в порт
                                    port.Write(modbusTransmit, 0, modbusTransmit.Length);

                                    byte[] response = new byte[255];

                                    try
                                    {
                                        port.Read(response, 0, 255);
                                        byte modbusReceive = Modbus.Response(response); // Принимаем modbus-ответ
                                        bool testOK = modbusReceive == (byte)((ViewDevice)d).Header.DeviceAddress ? true : false;
                                        if (testOK)
                                        {
                                            ((ViewDevice)d).Background = Brushes.Green;
                                            // Если будет использоваться в цикле, то оператор break нужен
                                            break;
                                        }
                                        else ((ViewDevice)d).Background = Brushes.Red;
                                    }
                                    catch (TimeoutException)
                                    {
                                        //MessageBox.Show("Время ожидания истекло");
                                        ((ViewDevice)d).Background = Brushes.Red;
                                        //return;
                                    }
                                    finally
                                    {
                                        port.Close();
                                    }
                                }
                            }
                            statusBar.Text = "Выполняется тестирование: " + ((ViewDevice)d).Header.ShieldName + ((ViewDevice)d).Header.ShieldNumber +", "+ ((ViewDevice)d).Header.DeviceType +"(Slave " + ((ViewDevice)d).Header.DeviceAddress+")";
                        }  
                        progressBar.Maximum = myPanel.Children.Count * s.ShieldPanel.Children.Count;
                        progressBar.Value++;                     
                        await Task.Delay(100);
                    }
                }
                statusBar.Text = "Готово";
                MessageBox.Show("Тестирование завершено!", "Тестирование", MessageBoxButton.OK);
            }
            catch { MessageBox.Show("Ошибка при выполнении тестирования!", "Тестирование", MessageBoxButton.OK); }
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Hidden;
            statusBar.Text = "";
        }
    }
}