using System;
using System.Net.WebSockets;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace IBMSpeechToText
{
    class Program
    {
       
        
        static void Main(string[] args)
        {
            //string[] audios;
            Console.WriteLine("Ingresa ruta de la carpeta de audios");
            audios = Directory.GetFiles(Console.ReadLine(), "*.wav");
            Console.WriteLine("Ingresa la ruta de la carpeta a guardar los Txt");
            txt = Console.ReadLine();
            
            Transcribe();
            Console.WriteLine("Precione una tecla para salir");
            Console.ReadLine();
        }
        static String username = "9bb258eb-ca70-40ee-b718-e3fd30fabd43";
        static String password = "iMg4dxC1wSev";
        static String[] audios;// = @"C:\Users\albertohernandez\Desktop\audios\Yess.wav";
        static String txt;
        static String name;
        static String names;
        static String fecha;
        static Uri url = new Uri("wss://stream.watsonplatform.net/speech-to-text/api/v1/recognize?model=es-ES_BroadbandModel");
        static ArraySegment<byte> openingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"content-type\": \"audio/wav\"}"
        ));
        static ArraySegment<byte> closingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"stop\"}"
        ));
        static void Transcribe()
        {
            var ws = new ClientWebSocket();
            ws.Options.Credentials = new NetworkCredential(username, password);
            ws.ConnectAsync(url, CancellationToken.None).Wait();
            Task.WaitAll(ws.SendAsync(openingMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleResults(ws));
            Task.WaitAll(SendAudio(ws), HandleResults(ws));
            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).Wait();
        }

        static async Task SendAudio(ClientWebSocket ws)
        {
            for (int i = 0; i < audios.Length; i++)
            {
                name = audios[i];
                using (FileStream fs = File.OpenRead(audios[i]))
                {
                    byte[] b = new byte[1024];
                    while (fs.Read(b, 0, b.Length) > 0)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                    await ws.SendAsync(closingMessage, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
        static async Task HandleResults(ClientWebSocket ws)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var segment = new ArraySegment<byte>(buffer);

                var result = await ws.ReceiveAsync(segment, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                int count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Eso es muy largo", CancellationToken.None);
                        return;
                    }

                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await ws.ReceiveAsync(segment, CancellationToken.None);
                    count += result.Count;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, count);
                names = Path.GetFileName(name);

                if (names == null)
                {
                } else{
                    DateTime dt = File.GetLastWriteTime(name);
                    names = names.Remove(names.Length - 4);
                    fecha = dt.ToString(" dddd,dd - MMMM - yyyy hh.mm.ss tt");
                    //using (System.IO.StreamWriter escritor = new System.IO.StreamWriter(txt + @"\" + names+ fecha + ".txt"))
                    //{
                    //    escritor.WriteLine(message);
                    //}
                }
                Console.WriteLine(message);
                JObject jObject = JObject.Parse(message);
               string textoo = (string)jObject["transcript"];
                if (IsDelimeter(message))
                {
                    return;
                }
            }
        }
        [DataContract]
        internal class ServiceState
        {
            [DataMember]
            public string state = "";
        }
        static bool IsDelimeter(String json)
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ServiceState));
            ServiceState obj = (ServiceState)ser.ReadObject(stream);
            return obj.state == "listening";
        }

    }
}