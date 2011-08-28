
namespace AlfaServer
{
    public class Configuration
    {
        private Configuration()
        {
        }

        private static Configuration _configuration;

        public static Configuration GetInstance()
        {
            if (_configuration == null)
            {
                _configuration = new Configuration();
            }

            return _configuration;
        }

        /// <summary>
        /// интервал между попытками открытиями порта
        /// </summary>
        /// <returns></returns>
        public int GetIntervalOpeningPorts()
        {
            return 1000000;
        }

        /// <summary>
        /// значения всех ключей с контроллера будут считываться до тех пор пока не будут получены все ключи с верной контрольной суммой,
        /// либо пока не будет превышено заданное число попыток
        /// </summary>
        public int CountReadingsAllKeys
        {
            get { return 5; }
        }

        /// <summary>
        /// количество ячеек для ключей
        /// </summary>
        public int CountKeysCells
        {
            get { return 12; }
        }

        /// <summary>
        /// интервалы ожидания между запросом на считывание ключей и операции считывания результата
        /// </summary>
        public int IntervalReadingAllKeys
        {
            get { return 500; }
        }

        /// <summary>
        /// интервал между операциями чтения
        /// </summary>
        public int IntervalSleepTime
        {
            get { return 200; }
        }

        public int MaxCountReadError
        {
            get { return 20; }
        }
    }
}
