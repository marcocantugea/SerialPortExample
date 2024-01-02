using System.IO.Ports;
using System;
using System.Threading;
using SerialPortCOM = System.IO.Ports.SerialPort;
namespace ClientSerialWebSocket.SerialPort
{
    public class SerialPortEngine : IDisposable
    {
        private SerialPortCOM _serialPort;
        private List<string> _readPort = new List<string>();


        public SerialPortCOM serialPort { get { return _serialPort; } }
        public IList<string> readPort { get { return _readPort; } }

        public event EventHandler PortReadAction;

        public SerialPortEngine(string portName, int buadRate = 9600, Parity parity = 0, int databits = 8, StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPortCOM(portName, 9600, Parity.None, 8, StopBits.One);
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(PortDataReceived);
            ;
        }

        public SerialPortEngine OpenPort()
        {
            _serialPort.Open();
            return this;
        }

        public SerialPortEngine ClosePort()
        {
            _serialPort.Close();
            return this;
        }

        public void Dispose()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }

        public SerialPortEngine setDataReceicedEventHandler(SerialDataReceivedEventHandler method)
        {
            _serialPort.DataReceived += method;
            return this;
        }

        public SerialPortEngine SendDataToPort(string data)
        {
            _serialPort.WriteLine(data);
            return this;
        }

        private void PortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _readPort.Add(_serialPort.ReadExisting());
            PortReadAction?.Invoke(this, e);
        }

    }
}
