using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace MAPCreator
{
    class Modbus
    {
        /// <summary>
        /// Вычисляет контрольную сумму modbus-сообщения
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <returns></returns>
        static byte[] CRC16(byte[] data)
        {
            ushort hash = 0xFFFF;
            const ushort polynom = 0xA001;
            foreach (ushort i in data)
            {
                hash ^= i;
                for (int j = 0; j <= 7; j++)
                {
                    bool val = Convert.ToBoolean(hash - 2 * (hash / 2));
                    hash >>= 1;
                    if (val == true) hash ^= polynom;
                }
            }
            return BitConverter.GetBytes(hash);
        }

        /// <summary>
        /// Меняет старший и младший байты местами
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static byte[] Swap16Bits(byte[] data)
        {
            int count = 0;
            if (data.Length % 2 == 0) count = data.Length;
            else count = data.Length + 1;
            byte[] result = new byte[count];
            for (int i = 0; i < count; i += 2)
            {
                if (i < count) result[i] = data[i + 1];
                else result[i] = 0;
                result[i + 1] = data[i];
            }
            return result;
        }

        /// <summary>
        /// Формирует Modbus-запрос
        /// </summary>
        /// <param name="id">Адрес ведомого устройства</param>
        /// <param name="function">Номер функции</param>
        /// <param name="address">Адрес первого регистра</param>
        /// <param name="quantity">Количество регистров</param>
        /// <returns></returns>
        public static byte[] Request(byte id, byte function, ushort address, ushort quantity)
        {
            // Преобразование в массив байтов и их перестановка
            byte[] ADR = BitConverter.GetBytes(address);
            Array.Reverse(ADR);

            byte[] QUANTITY = BitConverter.GetBytes(quantity);
            Array.Reverse(QUANTITY);

            // Формирование PDU для функций 01-04 (функции чтения)
            List<byte> PDU = new List<byte>() { function };
            PDU.AddRange(ADR);
            PDU.AddRange(QUANTITY);

            // Формирование ADU для RTU
            List<byte> ADU = new List<byte>() { id };
            ADU.AddRange(PDU);
            ADU.AddRange(CRC16(ADU.ToArray()));

            return ADU.ToArray();
        }

        /// <summary>
        /// Разбирает Modbus-ответ
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte Response(byte[] response)
        {
            if (response.Length == 0) return 0;
            int countByte = 0;
            for(int i = response.Length - 1; i > 0; i--)
            {
                if (response[i] != 0)
                {
                    countByte = i;
                    break;
                }
            }
            byte[] temp = new byte[countByte + 1];
            for(int i = 0; i < countByte+1; i++)
            {
                temp[i] = response[i];
            }


            //if (response.Length < 5) return null; // Если ответ меньше 5 байт - дальше не разбираем
            byte[] message = new byte[temp.Length - 2]; // Без CRC
            for (int i = 0; i < message.Length; i++)
            {
                message[i] = response[i];
            }
            byte[] CRC = CRC16(message);
            if (temp[temp.Length - 2] != CRC[0] || temp[temp.Length - 1] != CRC[1])
            {
                // Если контрольная сумма не совпадает, то дальше не разбираем
                return 0;
            }

            // Разбор ответа
            byte id = temp[0];
            /*byte function = response[1];
            if (function > 128) // Если код функции больше 128, то обрабатываем ошибку
            {
                byte error = response[2];
                string errorMessage;
                if (error == 1) errorMessage = "Функция не поддерживается";
                else if (error == 2) errorMessage = "Запрошенная область памяти не доступна";
                else if (error == 3) errorMessage = "Функция не поддерживает запрошенное количество данных";
                else if (error == 4) errorMessage = "Функция выполнена с ошибкой";
                else errorMessage = "Неизвестная ошибка";
                return null;
            }
            byte count = response[2]; // Количество байт данных
            byte[] data = new byte[count]; // Без id, function, count
            for (int i = 0; i < count; i++)
            {
                data[i] = response[i + 3];
            }*/
            return id;
        }

        /// <summary>
        /// Преобразует 8-битные данные в 16-битные
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static short[] ParseWord(byte[] data)
        {
            byte[] buffer = Swap16Bits(data); // Меняем старший и младший байты местами
            int count = buffer.Length;
            int index = 0;
            short[] result = new short[count / 2];
            for (int i = 0; i < count; i += 2)
            {
                result[index] = BitConverter.ToInt16(buffer, i);
                index++;
            }
            return result;
        }

        /// <summary>
        /// Преобразует данные в бинарный тип
        /// </summary>
        /// <param name="data"></param>
        /// <param name="convert">Преобразование из 16-битных данных (по умолчанию из 8-битных)</param>
        /// <returns></returns>
        public static short[] ParseBool(byte[] data, bool convert)
        {
            byte[] buffer;
            if (convert == true) buffer = Swap16Bits(data); // Меняем старший и младший байты местами
            else buffer = data;
            int count = buffer.Length;
            int index = 0;
            short[] result = new short[count * 8];
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((buffer[i] & (1 << j)) == 0) result[index] = 0;
                    else result[index] = 1;
                    index++;
                }
            }
            return result;
        }

        /// <summary>
        /// Обмен данными по TCP
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="outMessage"></param>
        /// <returns></returns>
        public static byte[] Exchange(string address, int port, byte[] data)
        {
            // Инициализация
            using (TcpClient client = new TcpClient(address, port))
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Отправка сообщения
                    stream.Write(data, 0, data.Length);
                    // Получение ответа
                    Thread.Sleep(100); // Ожидаем ответа 0,1 сек.
                    List<byte> readingData = new List<byte>();
                    while (stream.DataAvailable)
                    {
                        readingData.Add((byte)stream.ReadByte());
                    }
                    return readingData.ToArray();
                }
            }
        }

        /*
        static void Main111(string[] args)
        {
            
            
            Master Moxa = JsonConvert.DeserializeObject<Master>(config); // Создаем виртуальную модель опрашиваемых устройств
            while (true) // Делаем циклический опрос
            {
                for (int polling = 0; polling < Moxa.Slave.Length; polling++) // Последовательный опрос всех устройств
                {
                    byte[] modbusTransmit = ModbusMaster.Request // Формируем modbus-запрос
                        (
                            Moxa.Slave[polling].Id,
                            Moxa.Slave[polling].Function,
                            Moxa.Slave[polling].Address,
                            Moxa.Slave[polling].Quantity
                        );

                    byte[] modbusReceive = ModbusMaster.Response(Exchange(Moxa.Ip, Moxa.Port, modbusTransmit)); // Принимаем modbus-ответ
                    short[] result;
                    string timeStamp = $"{ DateTime.Now.ToString("D") };{ DateTime.Now.ToString("T")}";
                    if (Moxa.Slave[polling].Description.Length > Moxa.Slave[polling].Quantity) // Анализируем принятый ответ
                    {
                        result = ModbusMaster.ParseBool(modbusReceive, true);
                    }
                    else
                    {
                        result = ModbusMaster.ParseWord(modbusReceive);
                    }

                    for (int i = 0; i < Moxa.Slave[polling].Description.Length; i++)
                    {
                        string log = $"{Moxa.Slave[polling].Description[i]};{result[i].ToString()};{timeStamp}";
                    }
                    
                }
            }
        }*/
    }
}
