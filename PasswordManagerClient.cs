using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;

namespace PasswordManagerClient
{
    class PasswordManagerClient
    {
        private Client client;
        private RSACryptoServiceProvider csp;
        private string session;
        public PasswordManagerClient(IPAddress serverIP, int serverPort)
        {
            this.client = new Client(serverIP, serverPort);
            this.session = "-";

            this.csp = new RSACryptoServiceProvider(2048);
            int read;

            if(File.Exists("PublicKey"))
            {
                csp.ImportRSAPublicKey(File.ReadAllBytes("PublicKey"), out read);
            }
            else
            {
                File.WriteAllBytes("PublicKey", csp.ExportRSAPublicKey());
            }

            if (File.Exists("PrivateKey"))
            {
                csp.ImportRSAPrivateKey(File.ReadAllBytes("PrivateKey"), out read);
            }
            else
            {
                File.WriteAllBytes("PrivateKey", csp.ExportRSAPublicKey());
            }
        }

        public CommunicationProtocol CreateUser(string userName)
        {
            string publicKey = System.Convert.ToBase64String(File.ReadAllBytes("PublicKey"));

            string body = $"{{\"userName\":\"{userName}\",\"publicKey\":\"{publicKey}\"}}";
            byte[] bodyBytes = Encoding.ASCII.GetBytes(body);

            CommunicationProtocol answer = client.SendAndReceive("create_user", bodyBytes, "-", "json");

            return answer;
        }

        public CommunicationProtocol LoginRequest(string userName)
        {
            byte[] bodyBytes = Encoding.ASCII.GetBytes(userName);
            CommunicationProtocol answer = client.SendAndReceive("login_request", bodyBytes, "*", "ascii string");

            return answer;
        }

        public CommunicationProtocol LoginTest(byte[] encryptedNumber, string loginSession)
        {
            byte[] decryptedNumber = csp.Decrypt(encryptedNumber, false);

            CommunicationProtocol answer = client.SendAndReceive("login_test", decryptedNumber, loginSession, "bytes");

            return answer;
        }
    }
}
