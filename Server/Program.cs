using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server
{
    [Serializable]
    public class MessageInfo
    {
        public string Nickname { get; set; }
        public string IPAddress { get; set; }
        public string Time { get; set; }
        public string Text { get; set; }
    }
    class Program
    {
        private const int port = 8080;
        private static List<IPEndPoint> members = new List<IPEndPoint>();
        static void Main(string[] args)
        {
        Semaphore semaphore = new Semaphore(2, 2);
            UdpClient server = new UdpClient(port);
            IPEndPoint groupEP = null;

            try
            {
                while (true)
                {
                    Console.WriteLine("\tWaiting for a message...");
                    byte[] bytes = server.Receive(ref groupEP);

                    XmlSerializer xml = new XmlSerializer(typeof(MessageInfo));
                    MemoryStream stream = new MemoryStream();
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0;
                    var mi = (MessageInfo)xml.Deserialize(stream);
                    mi.IPAddress = groupEP.ToString();
                    bool isSuccesful;
                    if (mi.Text == "<JOIN>")
                    {
                        if (!semaphore.WaitOne(500))
                        {
                            MessageInfo info = new MessageInfo() { Nickname = mi.Nickname, Text = "The maximum number of users has been reached. Try to join later.", IPAddress = mi.IPAddress, Time = mi.Time };
                            XmlSerializer serializer = new XmlSerializer(typeof(MessageInfo));

                            MemoryStream ms = new MemoryStream();
                            xml.Serialize(ms, info);
                            ms.Position = 0;
                            byte[] minfo = new byte[ms.Length];
                            ms.Read(minfo, 0, (int)ms.Length);
                            server.Send(minfo, minfo.Length, groupEP);
                           
                           
                            continue;
                        }
                        isSuccesful = AddMember(groupEP);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Request to connect {mi.Nickname} at {DateTime.Now.ToShortTimeString()}\n");
                        
                        if (isSuccesful)
                        {
                            Console.WriteLine($"Operation completed succesful!\n");
                           
                            foreach (var m in members)
                            {
                                try
                                {
                                    MessageInfo info = new MessageInfo() { Nickname = mi.Nickname, Text = $"{mi.Nickname} has joined to chat.", IPAddress = mi.IPAddress, Time = mi.Time };
                                    XmlSerializer serializer = new XmlSerializer(typeof(MessageInfo));

                                    MemoryStream ms = new MemoryStream();
                                    xml.Serialize(ms, info);
                                    ms.Position = 0;
                                    byte[] minfo = new byte[ms.Length];
                                    ms.Read(minfo, 0, (int)ms.Length);
                                    server.Send(minfo, minfo.Length, m);

                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Error with {m}: {ex.Message}\n");
                                }
                            }
                        }
                    }
                    else if (mi.Text == "<LEAVE>")
                    {
                        isSuccesful = RemoveMember(groupEP);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Request to leave from {mi.Nickname} at {DateTime.Now.ToShortTimeString()}\n");
                        if (isSuccesful)
                        {
                            Console.WriteLine($"Operation completed succesful!\n");
                                   

                            foreach (var m in members)
                            {
                                
                                try
                                {
                                    MessageInfo info = new MessageInfo() { Nickname = mi.Nickname, Text = $"{mi.Nickname} left the chat.", IPAddress = mi.IPAddress, Time = mi.Time };
                                    XmlSerializer serializer = new XmlSerializer(typeof(MessageInfo));
                                 
                                    MemoryStream ms = new MemoryStream();
                                    xml.Serialize(ms, info);
                                    ms.Position = 0;
                                    byte[] minfo = new byte[ms.Length];
                                    ms.Read(minfo, 0, (int)ms.Length);
                                    server.Send(minfo,minfo.Length, m);
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Error with {m}: {ex.Message}\n");
                                }
                            }
                            semaphore.Release();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Message from {mi.Nickname} at {DateTime.Now.ToShortTimeString()}: {mi.Text}\n");
                        foreach (var m in members)
                        {
                            try
                            {                               
                                server.Send(bytes, bytes.Length, m);
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error with {m}: {ex.Message}\n");
                            }
                        }
                    }
                    Console.ResetColor();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
              
                server.Close();
            }
        }

        static bool AddMember(IPEndPoint endPoint)
        {
            var member = members.FirstOrDefault(m => m.ToString() == endPoint.ToString());
            if (member == null)
            {
                members.Add(endPoint);
                return true;
            }
            return false;
        }
        static bool CheckMember(IPEndPoint endPoint)
        {
            var member = members.FirstOrDefault(m => m.ToString() == endPoint.ToString());
            if (member != null)
            {              
                return true;
            }
            return false;
        }
        static bool RemoveMember(IPEndPoint endPoint)
        {
            var member = members.FirstOrDefault(m => m.ToString() == endPoint.ToString());
            if (member != null)
            {
                members.Remove(member);
                return true;
            }
            return false;
        }
    }
}