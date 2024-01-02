using ClientSerialWebSocket.SerialPort;
using ClientSerialWebSocket.Websocket;
using System.IO.Ports;
using System.Net;
using System.Runtime.CompilerServices;


public class Program
{
    public static SerialPortEngine _serialPortEngine;
    public static WebSocketServerEngine _webSocketServerEngine;
    public static async Task Main(string[] args)
    {
        _serialPortEngine = new SerialPortEngine("COM3");
        _webSocketServerEngine = new WebSocketServerEngine(IPAddress.Parse("127.0.0.1"));
        Console.WriteLine("Websocket starting in t http://127.0.0.1:4452");

        _= _webSocketServerEngine.Start();
        Console.WriteLine("Client Connected");
        Console.WriteLine("Open and Reading Serial Port COM3");
        _serialPortEngine.OpenPort();
        _serialPortEngine.PortReadAction += PrintSerialPort;

        bool closeTerminal=false;

        while (!closeTerminal)
        {
            var read= Console.ReadLine();
            if (string.IsNullOrEmpty(read)) continue;
            if(read =="quit") closeTerminal = true;
            if (read.Contains("ws:"))
            {
                _webSocketServerEngine.SendDataMessate(read.Replace("ws:",""));
                continue;
            }
            _serialPortEngine.SendDataToPort(read);
        }

        
      
    }

    private static void PrintSerialPort(object sender,EventArgs e)
    {
        Console.WriteLine(_serialPortEngine.readPort.LastOrDefault());
        _webSocketServerEngine.SendDataMessate(_serialPortEngine.readPort.LastOrDefault());
    }
}