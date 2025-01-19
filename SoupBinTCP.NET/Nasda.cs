using System;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;

namespace NasdaqMME.Client
{
    public class MMEMessage
    {
        public long ClientSequenceNr { get; set; }
        public short MessageGroup { get; set; }
        public short MessageId { get; set; }
        public byte PartitionId { get; set; }
        public byte[] Payload { get; set; }
    }

    public class ReferencePriceMessage
    {
        public int ActorId { get; set; }
        public int OrderBookId { get; set; }
        public byte ReferencePriceSource { get; set; }
        public long ReferencePrice { get; set; }
        public long Timestamp { get; set; }
    }

    public class MMEClient
    {
        private readonly SoupBinTCP.NET.Client _soupBinClient;
        private long _sequenceNumber = 1;

        public MMEClient(string host, int port, string username, string password)
        {
            _soupBinClient = new SoupBinTCP.NET.Client(
                IPAddress.Parse(host),
                port,
                new SoupBinTCP.NET.LoginDetails
                {
                    Username = username,
                    Password = password
                },
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
            // Construct MME message
            var mmeMessage = new MMEMessage
            {
                ClientSequenceNr = _sequenceNumber++,
                MessageGroup = 13, // Reference Price message group
                MessageId = 4,     // Reference Price message ID
                PartitionId = 0,   // Default partition
                Payload = EncodeReferencePricePayload(message)
            };

            // Encode full message
            var encodedMessage = EncodeMMEMessage(mmeMessage);

            // Send via SoupBinTCP
            await _soupBinClient.Send(encodedMessage);
        }

        private byte[] EncodeMMEMessage(MMEMessage message)
        {
            var messageSize = 8 + 2 + 2 + 1 + message.Payload.Length;
            var buffer = new byte[messageSize];
            var offset = 0;

            // Write client sequence number (8 bytes)
            BitConverter.GetBytes(message.ClientSequenceNr).CopyTo(buffer, offset);
            offset += 8;

            // Write message group (2 bytes)
            BitConverter.GetBytes(message.MessageGroup).CopyTo(buffer, offset);
            offset += 2;

            // Write message ID (2 bytes)
            BitConverter.GetBytes(message.MessageId).CopyTo(buffer, offset);
            offset += 2;

            // Write partition ID (1 byte)
            buffer[offset++] = message.PartitionId;

            // Write payload
            message.Payload.CopyTo(buffer, offset);

            return buffer;
        }

        private byte[] EncodeReferencePricePayload(ReferencePriceMessage message)
        {
            var buffer = new byte[28]; // Size of all fields
            var offset = 0;

            // Write ActorId (4 bytes)
            BitConverter.GetBytes(message.ActorId).CopyTo(buffer, offset);
            offset += 4;

            // Write OrderBookId (4 bytes)
            BitConverter.GetBytes(message.OrderBookId).CopyTo(buffer, offset);
            offset += 4;

            // Write ReferencePriceSource (1 byte)
            buffer[offset++] = message.ReferencePriceSource;

            // Write ReferencePrice (8 bytes)
            BitConverter.GetBytes(message.ReferencePrice).CopyTo(buffer, offset);
            offset += 8;

            // Write Timestamp (8 bytes)
            BitConverter.GetBytes(message.Timestamp).CopyTo(buffer, offset);

            return buffer;
        }
    }

    public class MMEClientListener : SoupBinTCP.NET.IClientListener
    {
        public async Task OnConnect()
        {
            Console.WriteLine("Connected to MME Gateway");
        }

        public async Task OnMessage(byte[] message)
        {
            // Handle response messages
            if (message.Length >= 3)
            {
                var messageGroup = BitConverter.ToInt16(message, 0);
                var messageId = BitConverter.ToInt16(message, 2);

                if (messageGroup == 26 && messageId == 1) // Commit Response Message
                {
                    var statusCode = BitConverter.ToInt32(message, 4);
                    var clientSequenceNr = BitConverter.ToInt64(message, 8);

                    Console.WriteLine($"Received response for message {clientSequenceNr}, status: {statusCode}");
                }
            }
        }

        public async Task OnLoginAccept(string session, ulong sequenceNumber)
        {
            Console.WriteLine($"Login accepted. Session: {session}, Sequence: {sequenceNumber}");
        }

        public async Task OnLoginReject(char rejectReasonCode)
        {
            Console.WriteLine($"Login rejected. Reason: {rejectReasonCode}");
        }

        public async Task OnDisconnect()
        {
            Console.WriteLine("Disconnected from MME Gateway");
        }

        public Task OnDebug(string message)
        {
            Console.WriteLine($"Debug: {message}");
            return Task.CompletedTask;
        }
    }
}