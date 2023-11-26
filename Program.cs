using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;

namespace PasswordManagerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            PasswordManagerClient passwordManagerClient = new PasswordManagerClient(serverIP, 8080);

            CommunicationProtocol answer = passwordManagerClient.LoginRequest("Niv");
            answer = passwordManagerClient.LoginTest(answer.Body, answer.GetHeaderValue("Session"));

            string bodyStr = Encoding.ASCII.GetString(answer.Body);
            Console.WriteLine(bodyStr);

            Console.ReadLine();
        }

    }
}
