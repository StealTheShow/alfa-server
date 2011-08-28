using System;
using System.Collections.Generic;
using System.Threading;
using AlfaServer.models;

namespace AlfaServer
{
    class ConsoleCommand
    {
        private static ConsoleCommand _instance;

        public static ConsoleCommand GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ConsoleCommand();
            }

            return _instance;
        }

        public void SetOrRemoveKey(Floor floor, byte[] key)
        {
            //Console.Clear();
            Console.WriteLine("add/rm");

            string command = Console.ReadLine();
            
            if (command == "add")
            {
                SetKey(floor, key);
            }
            if (command == "rm")
            {
                UnsetKey(floor, key);
            }
        }

        private void SetKey(Floor floor, byte[] key)
        {
            Console.Clear();
            Console.WriteLine("добавление ключа контроллера");

            Console.WriteLine("ввеите номер контролера двери");

            string command = Console.ReadLine();

            byte number = 0;
            if (command != null)
            {
                try
                {
                    number = byte.Parse(command);
                }
                catch (Exception)
                {
                    Console.WriteLine("не удалось получить номер контроллера");
                    return;
                }

            }

            Thread.Sleep(200);
            Dictionary<byte, byte[]> keys = floor.GetAllKeys(number);

            Console.Clear();

            Console.WriteLine("Список ключей");
            foreach (KeyValuePair<byte, byte[]> pair in keys)
            {
                Console.WriteLine("номер ключа {0} со значением {1}", pair.Key, BitConverter.ToString(pair.Value));
            }

            Console.WriteLine("Введите номер ключа");
            command = Console.ReadLine();

            byte keyNumber = 0;
            if (command != null)
            {
                try
                {
                    keyNumber = byte.Parse(command);
                }
                catch (Exception)
                {
                    Console.WriteLine("не удалось получить номер ключа");
                    return;
                }
            }

            Thread.Sleep(200);
            floor.SetKey(number, keyNumber, key);

            Thread.Sleep(200);

            keys = floor.GetAllKeys(number);

            Console.WriteLine("новый список ключей");
            foreach (KeyValuePair<byte, byte[]> pair in keys)
            {
                Console.WriteLine("номер ключа {0} со значением {1}", pair.Key, BitConverter.ToString(pair.Value));
            }
        }

        private void UnsetKey(Floor floor, byte[] key)
        {
            Console.Clear();
            Console.WriteLine("удаление ключа контроллера");

            Console.WriteLine("ввеите номер контролера двери");

            string command = Console.ReadLine();

            byte number = 0;
            if (command != null)
            {
                try
                {
                    number = byte.Parse(command);
                }
                catch (Exception)
                {
                    Console.WriteLine("не удалось получить номер контроллера");
                    return;
                }

            }

            Thread.Sleep(200);
            Dictionary<byte, byte[]> keys = floor.GetAllKeys(number);

            Console.Clear();

            Console.WriteLine("Список ключей");
            foreach (KeyValuePair<byte, byte[]> pair in keys)
            {
                Console.WriteLine("номер ключа {0} со значением {1}", pair.Key, BitConverter.ToString(pair.Value));
            }

            Console.WriteLine("Введите номер ключа");
            command = Console.ReadLine();

            byte keyNumber = 0;
            if (command != null)
            {
                try
                {
                    keyNumber = byte.Parse(command);
                }
                catch (Exception)
                {
                    Console.WriteLine("не удалось получить номер ключа");
                    return;
                }
            }

            Thread.Sleep(200);
            floor.UnsetKey(number, keyNumber);

            Thread.Sleep(200);

            keys = floor.GetAllKeys(number);

            Console.WriteLine("новый список ключей");
            foreach (KeyValuePair<byte, byte[]> pair in keys)
            {
                Console.WriteLine("номер ключа {0} со значением {1}", pair.Key, BitConverter.ToString(pair.Value));
            }
        }
    }
}
