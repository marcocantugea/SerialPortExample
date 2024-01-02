using System.IO.Ports;
using System;
using  System.Threading;
using System.Net;
using System.Text;
using Microsoft.VisualBasic;
public class Program : IDisposable
{

    public static SerialPort _serialPort= new SerialPort("COM3",9600,Parity.None,8,StopBits.One);
    public static bool _exit=false;
    public static List<string> _readPort=new List<string>();
   private async static Task Main(string[] args)
    {
       
        //_serialPort.Handshake = Handshake.None;
        //var listener = new HttpListener();
        //listener.Prefixes.Add("http://localhost:8086/");
        //listener.Start();
        //Console.WriteLine("Start websocket at http://localhost:8086/");
        try
        {

            _serialPort.Open();
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Sending msg.");
                _serialPort.WriteLine(String.Format("<{0}>: {1}", "P01", "message sent"));
            }

            Console.WriteLine("Startig to read data:");

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
         
            bool endProcess=false;

            while (!endProcess)
            {
                if (_readPort.Contains("quit"))
                {
                    endProcess = true;
                }
            }
        }
        catch (System.Exception)
        {
            _serialPort.Close();
            throw;
        }
        finally
        {
            _serialPort.Close();
        }
    }

    private static void port_DataReceived(object sender, SerialDataReceivedEventArgs e) 
    { 
       // Show all the incoming data in the port's buffer
       _readPort.Add(_serialPort.ReadExisting().ToString());
        Console.WriteLine(_readPort.LastOrDefault().ToString());
    }

    public void Dispose()
    {
        _serialPort.Close();
    }

    static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        var listener = context.Response;
        listener.StatusCode = 101;
        listener.StatusDescription = "Switching Protocols";
        listener.Headers.Add("Upgrade", "websocket");
        listener.Headers.Add("Connection", "Upgrade");
        listener.Headers.Add("Sec-WebSocket-Accept", GetSecWebSocketAcceptKey(context.Request.Headers["Sec-WebSocket-Key"]));

        var webSocketContext = await context.AcceptWebSocketAsync(null);

        var socket = webSocketContext.WebSocket;

        Console.WriteLine("Cliente conectado");

        var buffer = new byte[1024];

        while (socket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
            {
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensaje recibido: {message}");

                // Responder al cliente
                var responseMessage = $"Respuesta del servidor: {message}";
                var responseBuffer = System.Text.Encoding.UTF8.GetBytes(responseMessage);
                await socket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        Console.WriteLine("Cliente desconectado");
    }

    static string GetSecWebSocketAcceptKey(string secWebSocketKey)
    {
        const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        string concatenated = secWebSocketKey + WebSocketGuid;
        byte[] sha1Hash = System.Security.Cryptography.SHA1.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(concatenated));
        return Convert.ToBase64String(sha1Hash);
    }
}

