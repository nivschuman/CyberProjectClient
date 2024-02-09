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
            passwordManagerClient.ImportRSAKeys("PublicKey", "PrivateKey");

            CommunicationProtocol answer;

            //create new user
            //answer = passwordManagerClient.CreateUser("Niv");

            //login
            answer = passwordManagerClient.LoginRequest("Niv");
            string loginSession = answer.GetHeaderValue("Session");
            answer = passwordManagerClient.LoginTest(answer.Body, loginSession);

            //set password
            answer = passwordManagerClient.SetPassword("Youtube.com", "Password123", loginSession);

            //get password
            answer = passwordManagerClient.GetPassword("Youtube.com", loginSession);
            Console.WriteLine(passwordManagerClient.DecryptPassword(answer.Body));

            //get sources
            answer = passwordManagerClient.GetSources(loginSession);
            Console.WriteLine(Encoding.ASCII.GetString(answer.Body));

            //delete password
            answer = passwordManagerClient.DeletePassword("Youtube.com", loginSession);
            Console.WriteLine(Encoding.ASCII.GetString(answer.Body));

            Console.ReadLine();
        }

    }
}
