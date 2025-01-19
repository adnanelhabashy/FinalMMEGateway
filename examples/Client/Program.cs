using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NasdaqMME.Client;
using SoupBinTCP.NET;
using SoupBinTCP.NET.Messages;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Console.WriteLine("Starting client");
            //var client = new SoupBinTCP.NET.Client(IPAddress.Parse("127.0.0.1"), 5500, new LoginDetails()
            //{
            //    Password = "password",
            //    Username = "user"
            //}, new ClientListener());
            //client.Start();
            //var command = Console.ReadLine();
            //while (command != "x")
            //{
            //    Message message;
            //    switch (command)
            //    {
            //            case "d":
            //                message = new Debug("debug message!!");
            //                break;
            //            case "s":
            //                message = new UnsequencedData(Encoding.ASCII.GetBytes("unsequenced"));
            //                break;
            //            case "l":
            //                message = new LoginRequest("user", "password");
            //                break;
            //            default:
            //                message = new Debug("default");
            //                break;
            //    }
            //    await client.Send(message.Bytes);
            //    command = Console.ReadLine();
            //}
            // client.Shutdown();


            var client = new MMEClient(
    host: "127.0.0.1",
    port: 19000, // Your MME gateway port
    username: "EGMG02",
    password: "DlfroV3k?p"
);

            client.Start();



           await Task.Delay(10000);

            var referencePrice = new ReferencePriceMessage
            {
                ActorId = 388,
                OrderBookId = 57,
                ReferencePriceSource = 1, // External reference price
                ReferencePrice = 55000,
                Timestamp = 0 // System will use current time
            };

            await client.SendReferencePriceMessage(referencePrice);

            await Task.Delay(10000);

        }
    }
}