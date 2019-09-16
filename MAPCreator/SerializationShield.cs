using System;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MAPCreator
{
    public class SerializationShield
    {
        [Serializable]
        public class Shield
        {
            public string ShieldName { get; set; }
            public int ShieldNumber { get; set; }
            public List<Device> Devices { get; set; }
        }

        [Serializable]
        public class Device
        {
            public List<Signal> DeviceSignal { get; set; }
            public Header Header { get; set; }
        }

        public void Write(string path, WrapPanel config)
        {
            List<Shield> configFile = new List<Shield>();

            foreach (ViewShield s in config.Children)
            {
                Shield tempS = new Shield();
                tempS.ShieldName = s.SType;
                tempS.ShieldNumber = s.SNum;
                tempS.Devices = new List<Device>();

                foreach (object d in s.ShieldPanel.Children)
                {
                    Device tempD = new Device();
                    if (d is ViewDevice)
                    {
                        tempD.Header = new Header()
                        { 
                            ShieldName = ((ViewDevice)d).Header.ShieldName,
                            ShieldNumber = ((ViewDevice)d).Header.ShieldNumber,
                            DeviceType =((ViewDevice)d).Header.DeviceType,
                            DeviceBaudRate = ((ViewDevice)d).Header.DeviceBaudRate,
                            DeviceParity = ((ViewDevice)d).Header.DeviceParity,
                            DeviceAddress = ((ViewDevice)d).Header.DeviceAddress
                        };

                        tempD.DeviceSignal = new List<Signal>();
                        foreach (Signal sg in ((ViewDevice)d).Signal)
                        {
                            tempD.DeviceSignal.Add(new Signal()
                            {
                                FunctionCode = sg.FunctionCode,
                                SignalName = sg.SignalName,
                                DataType = sg.DataType,
                                RegisterAddress = sg.RegisterAddress,
                                BitNumber = sg.BitNumber
                            });                           
                        }
                        tempS.Devices.Add(tempD);
                    }
                }
                configFile.Add(tempS);
            }

            Shield[] tempShield = configFile.ToArray();
            FileStream stream = File.Create(path);
            XmlSerializer serializer = new XmlSerializer(typeof(Shield[]));
            serializer.Serialize(stream, tempShield);
            stream.Close();
        }

        public void Read(string path, WrapPanel config, RoutedEventHandler UpdateForm_Click)
        {
            FileStream stream = File.OpenRead(path);
            XmlSerializer serializer = new XmlSerializer(typeof(Shield[]));
            Shield[] tempShield = (Shield[])serializer.Deserialize(stream);
            config.Children.Clear();
            
            foreach (Shield s in tempShield)
            {
                ViewShield tempViewShield = new ViewShield(s.ShieldName, s.ShieldNumber, UpdateForm_Click);
                config.Children.Add(tempViewShield);
                foreach (Device d in s.Devices)
                {
                    Header tempHeader = new Header();
                    tempHeader.DeviceType = d.Header.DeviceType;
                    tempHeader.DeviceAddress = d.Header.DeviceAddress;
                    tempHeader.DeviceBaudRate = d.Header.DeviceBaudRate;
                    tempHeader.DeviceParity = d.Header.DeviceParity;
                    tempHeader.ShieldName = s.ShieldName;
                    tempHeader.ShieldNumber = s.ShieldNumber;

                    ViewDevice tempViewDevice = new ViewDevice(tempHeader,UpdateForm_Click);
                    tempViewShield.AddDevice(d.Header.DeviceType, d.Header.DeviceAddress, d.Header.DeviceBaudRate, d.Header.DeviceParity, UpdateForm_Click);
                    int countDevice = tempViewShield.ShieldPanel.Children.Count;
                    foreach (Signal sg in d.DeviceSignal)
                    {
                        Signal tempSignal = new Signal();
                        tempSignal.FunctionCode = sg.FunctionCode;
                        tempSignal.SignalName = sg.SignalName;
                        tempSignal.DataType = sg.DataType;
                        tempSignal.RegisterAddress = sg.RegisterAddress;
                        tempSignal.BitNumber = sg.BitNumber;

                        ((ViewDevice)tempViewShield.ShieldPanel.Children[countDevice-1]).Signal.Add(tempSignal);
                    }
                }
            }
        }
    }
}
