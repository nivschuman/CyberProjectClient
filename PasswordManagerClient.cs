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
        private string keysDirectoryName;

        public PasswordManagerClient(IPAddress serverIP, int serverPort) : this(serverIP, serverPort, "keys")
        {
        }
        public PasswordManagerClient(IPAddress serverIP, int serverPort, string keysDirectoryName)
        {
            client = new Client(serverIP, serverPort);
            csp = new RSACryptoServiceProvider(2048);

            this.keysDirectoryName = keysDirectoryName;

            if(!Directory.Exists(keysDirectoryName))
            {
                Directory.CreateDirectory(keysDirectoryName);
            }
        }

        public void ImportRSAKeys(string publicKeyFileName, string privateKeyFileName)
        {
            int read;

            if (File.Exists(publicKeyFileName))
            {
                csp.ImportRSAPublicKey(File.ReadAllBytes(publicKeyFileName), out read);
            }

            if (File.Exists(privateKeyFileName))
            {
                csp.ImportRSAPrivateKey(File.ReadAllBytes(privateKeyFileName), out read);
            }
        }

        public void CreateNewRSAKeys(string publicKeyFileName, string privateKeyFileName)
        {
            csp = new RSACryptoServiceProvider(2048);

            string publicKeyPath = Path.Combine(keysDirectoryName, publicKeyFileName);
            string privateKeyPath = Path.Combine(keysDirectoryName, privateKeyFileName);

            File.WriteAllBytes(publicKeyPath, csp.ExportRSAPublicKey());
            File.WriteAllBytes(privateKeyPath, csp.ExportRSAPrivateKey());
        }

        public CommunicationProtocol CreateUser(string userName)
        {
            string publicKey = System.Convert.ToBase64String(csp.ExportRSAPublicKey());

            string body = $"{{\"userName\":\"{userName}\",\"publicKey\":\"{publicKey}\"}}";
            byte[] bodyBytes = Encoding.ASCII.GetBytes(body);

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("create_user", bodyBytes, "-", "json");
            }
            catch(SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public CommunicationProtocol LoginRequest(string userName)
        {
            byte[] bodyBytes = Encoding.ASCII.GetBytes(userName);
            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("login_request", bodyBytes, "*", "ascii");
            }
            catch(SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public CommunicationProtocol LoginTest(byte[] encryptedNumber, string loginSession)
        {
            byte[] decryptedNumber = csp.Decrypt(encryptedNumber, false);

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("login_test", decryptedNumber, loginSession, "bytes");
            }
            catch (SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public CommunicationProtocol GetSources(string loginSession)
        {
            byte[] emptyBody = new byte[0];

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("get_sources", emptyBody, loginSession);
            }
            catch (SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public CommunicationProtocol GetPassword(string source, string loginSession)
        {
            byte[] bodyBytes = Encoding.ASCII.GetBytes(source);

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("get_password", bodyBytes, loginSession, "ascii");
            }
            catch(SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }
            

            return answer;
        }

        public CommunicationProtocol SetPassword(string source, string password, string loginSession)
        {
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] encodedPassword = csp.Encrypt(passwordBytes, false);
            string encodedPasswordStr = System.Convert.ToBase64String(encodedPassword);

            string bodyJson = $"{{\"source\": \"{source}\", \"password\": \"{encodedPasswordStr}\"}}";
            byte[] bodyBytes = Encoding.ASCII.GetBytes(bodyJson);

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("set_password", bodyBytes, loginSession, "json");
            }
            catch(SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public CommunicationProtocol DeletePassword(string source, string loginSession)
        {
            byte[] bodyBytes = Encoding.ASCII.GetBytes(source);

            CommunicationProtocol answer = client.SendAndReceive("delete_password", bodyBytes, loginSession, "ascii");

            return answer;
        }

        public CommunicationProtocol DeleteUser(string loginSession)
        {
            byte[] emptyBody = new byte[0];

            CommunicationProtocol answer;

            try
            {
                answer = client.SendAndReceive("delete_user", emptyBody, loginSession);
            }
            catch(SocketException e)
            {
                PMClientException pme = new PMClientException(e);

                throw pme;
            }

            return answer;
        }

        public string DecryptPassword(byte[] encryptedPassword)
        {
            byte[] decryptedPassword = csp.Decrypt(encryptedPassword, false);
            string decryptedPasswordStr = Encoding.ASCII.GetString(decryptedPassword);

            return decryptedPasswordStr;
        }
    }
}
