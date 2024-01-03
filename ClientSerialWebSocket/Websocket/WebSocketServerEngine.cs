using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ClientSerialWebSocket.Websocket
{
    public class WebSocketServerEngine : IDisposable
    {
        private TcpListener _server;
        private TcpClient _client;
        private  NetworkStream _stream;
        private List<string> _receivedMessages=new List<string>();

        public List<string> ReceivedMessages { get { return _receivedMessages; }}

        public WebSocketServerEngine(IPAddress address,int port=4452)
        {
            _server= new TcpListener(address, port);
        }

        public async  Task<WebSocketServerEngine> Start()
        {
            _server.Start();
            _client= _server.AcceptTcpClient();
            _stream= _client.GetStream();

            _ = Task.Run(async () =>
            {
                await Process();
            });

            return this;
        }

        private async Task Process()
        {
            while (true)
            {
                while (!_stream.DataAvailable) ;
                while (_client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[_client.Available];
                _stream.Read(bytes, 0, bytes.Length);
                string content = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
                {
                    await Handshake(content);
                    continue;
                }

                bool fin = (bytes[0] & 0b10000000) != 0,
                mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    offset = 2;
                ulong msglen = bytes[1] & (ulong)0b01111111;

                if (msglen == 126)
                {
                    // bytes are reversed because websocket will print them in Big-Endian, whereas
                    // BitConverter will want them arranged in little-endian on windows
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                    // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                    // websocket frame available through client.Available).
                    msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                    offset = 10;
                }

                if (msglen != 0 && mask)
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (ulong i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(bytes[offset + (int)i] ^ masks[i % 4]);

                    string text = Encoding.UTF8.GetString(decoded);
                    _receivedMessages.Add(text);    
                }
                else
                    Console.WriteLine("mask bit not set");
            }

            
        }

        public async Task SendDataMessage(string message)
        {
            byte[] messageInBytes=Encoding.UTF8.GetBytes(message);
            SendMessage(messageInBytes);
        }

        private async Task Handshake(string content) {

            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            string swk = Regex.Match(content, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

            _stream.Write(response, 0, response.Length);
        }

       private void SendMessage( byte[] payload, bool isBinary = false, bool masking = false)
        {
            var mask = new byte[4];
            SendMessage( payload, isBinary ? 0x2 : 0x1, masking, mask);
        }
        private void SendMessage(byte[] payload, int opcode, bool masking, byte[] mask)
        {
            if (masking && mask == null) throw new ArgumentException(nameof(mask));

            using (var packet = new MemoryStream())
            {
                byte firstbyte = 0b0_0_0_0_0000; // fin | rsv1 | rsv2 | rsv3 | [ OPCODE | OPCODE | OPCODE | OPCODE ]

                firstbyte |= 0b1_0_0_0_0000; // fin
                                             //firstbyte |= 0b0_1_0_0_0000; // rsv1
                                             //firstbyte |= 0b0_0_1_0_0000; // rsv2
                                             //firstbyte |= 0b0_0_0_1_0000; // rsv3

                firstbyte += (byte)opcode; // Text
                packet.WriteByte(firstbyte);

                // Set bit: bytes[byteIndex] |= mask;

                byte secondbyte = 0b0_0000000; // mask | [SIZE | SIZE  | SIZE  | SIZE  | SIZE  | SIZE | SIZE]

                if (masking)
                    secondbyte |= 0b1_0000000; // mask

                if (payload.LongLength <= 0b0_1111101) // 125
                {
                    secondbyte |= (byte)payload.Length;
                    packet.WriteByte(secondbyte);
                }
                else if (payload.LongLength <= UInt16.MaxValue) // If length takes 2 bytes
                {
                    secondbyte |= 0b0_1111110; // 126
                    packet.WriteByte(secondbyte);

                    var len = BitConverter.GetBytes(payload.LongLength);
                    Array.Reverse(len, 0, 2);
                    packet.Write(len, 0, 2);
                }
                else // if (payload.LongLength <= Int64.MaxValue) // If length takes 8 bytes
                {
                    secondbyte |= 0b0_1111111; // 127
                    packet.WriteByte(secondbyte);

                    var len = BitConverter.GetBytes(payload.LongLength);
                    Array.Reverse(len, 0, 8);
                    packet.Write(len, 0, 8);
                }

                if (masking)
                {
                    packet.Write(mask, 0, 4);
                    payload = ApplyMask(payload, mask);
                }

                // Write all data to the packet
                packet.Write(payload, 0, payload.Length);

                var finalPacket = packet.ToArray();
                Console.WriteLine($@"SENT: {BitConverter.ToString(finalPacket)}");

                // Send the packet
                foreach (var b in finalPacket)
                    _stream.WriteByte(b);
            }
        }

        static byte[] ApplyMask(IReadOnlyList<byte> msg, IReadOnlyList<byte> mask)
        {
            var decoded = new byte[msg.Count];
            for (var i = 0; i < msg.Count; i++)
                decoded[i] = (byte)(msg[i] ^ mask[i % 4]);
            return decoded;
        }

        public void Dispose()
        {
            if (_server != null) {
                _server.Stop();
                _server = null;
            }
        }
    }
}
