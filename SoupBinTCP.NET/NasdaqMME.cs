using System;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using SoupBinTCP.NET;
using SoupBinTCP.NET.Messages;
using Newtonsoft.Json;
using System.Text.Json;
using SoupBinTCP.NET;
namespace NasdaqMME
{
    public class MMEGatewayClient
    {
        private readonly SoupBinTCP.NET.Client _soupBinClient;
        private readonly string _username;
        private readonly string _password;
        private long _clientSequenceNumber;

        public MMEGatewayClient(string hostname, int port, string username, string password)
        {
            _username = username;
            _password = password;
            _clientSequenceNumber = 1;

            _soupBinClient = new SoupBinTCP.NET.Client(
                IPAddress.Parse(hostname),
                port,
                new LoginDetails { Username = username, Password = password },
                new MMEClientListener()
            );
        }

        public void Start()
        {
            _soupBinClient.Start();
        }

        public void Stop()
        {
            _soupBinClient.Shutdown();
        }

        public async Task SendReferencePriceMessage(ReferencePriceMessage message)
        {
            // Create binary message according to MME Protocol specification
             var memoryStream = new System.IO.MemoryStream();
             var writer = new System.IO.BinaryWriter(memoryStream);

            // Write client sequence number (8 bytes)
            writer.Write(_clientSequenceNumber++);

            // Write message group (2 bytes) - 13 for Reference Price
            writer.Write((short)13);

            // Write message ID (2 bytes) - 4 for Reference Price
            writer.Write((short)4);

            // Write partition ID (1 byte)
            writer.Write(Convert.ToByte(message.PartitionId));

            // Write actor ID (4 bytes)
            writer.Write(message.ActorId);

            // Write order book ID (4 bytes)
            writer.Write(message.OrderBookId);

            // Write reference price source (1 byte)
            writer.Write(Convert.ToByte(message.ReferencePriceSource));

            // Write reference price (8 bytes)
            writer.Write(message.ReferencePrice);

            // Write timestamp (8 bytes)
            writer.Write(message.Timestamp);

            await _soupBinClient.Send(memoryStream.ToArray());
        }
    }

    public class ReferencePriceMessage
    {
        public byte PartitionId { get; set; }
        public int ActorId { get; set; }
        public int OrderBookId { get; set; }
        public byte ReferencePriceSource { get; set; }
        public long ReferencePrice { get; set; }
        public long Timestamp { get; set; }

        public static ReferencePriceMessage FromJson(string json)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
            return new ReferencePriceMessage
            {
                PartitionId = byte.Parse(data.GetProperty("partitionId").GetString()),
                ActorId = int.Parse(data.GetProperty("actorId").GetString()),
                OrderBookId = int.Parse(data.GetProperty("orderBookId").GetString()),
                ReferencePriceSource = byte.Parse(data.GetProperty("referencePriceSource").GetString()),
                ReferencePrice = long.Parse(data.GetProperty("referencePrice").GetString()),
                Timestamp = 0 // System will use current timestamp
            };
        }
    }

    public class MMEClientListener : IClientListener
    {
        public async Task OnConnect()
        {
            Console.WriteLine("Connected to MME Gateway");
        }

        public async Task OnDebug(string message)
        {
            Console.WriteLine($"Debug: {message}");
        }

        public async Task OnDisconnect()
        {
            Console.WriteLine("Disconnected from MME Gateway");
        }

        public async Task OnLoginAccept(string session, ulong sequenceNumber)
        {
            Console.WriteLine($"Login accepted. Session: {session}, Sequence: {sequenceNumber}");
        }

        public async Task OnLoginReject(char rejectReasonCode)
        {
            Console.WriteLine($"Login rejected. Reason: {rejectReasonCode}");
        }

        public async Task OnMessage(byte[] message)
        {
            // Handle response messages from the gateway
             var memoryStream = new System.IO.MemoryStream(message);
             var reader = new System.IO.BinaryReader(memoryStream);

            short messageGroup = reader.ReadInt16();
            short messageId = reader.ReadInt16();

            if (messageGroup == 26 && messageId == 1) // Commit Response Message
            {
                byte partitionId = reader.ReadByte();
                int statusCode = reader.ReadInt32();
                long clientSequenceNr = reader.ReadInt64();

                Console.WriteLine($"Received response: PartitionId={partitionId}, StatusCode={statusCode}, SequenceNr={clientSequenceNr}");
            }
        }
    }
}