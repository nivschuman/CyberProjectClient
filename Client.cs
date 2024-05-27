﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace PasswordManagerClient
{
    class Client
    {
        private IPAddress serverIP;
        private IPEndPoint serverEndPoint;
        private int receiveTimeout;
        private bool withSSL;

        public Client(IPAddress serverIP, int serverPort, int receiveTimeout, bool withSSL)
        {
            this.serverIP = serverIP;
            this.serverEndPoint = new IPEndPoint(serverIP, serverPort);
            this.receiveTimeout = receiveTimeout;
            this.withSSL = withSSL;
        }

        public Client(IPAddress serverIP, int serverPort) : this(serverIP, serverPort, 120000, true)
        {
        }

        public Client(IPAddress serverIP, int serverPort, bool withSSL) : this(serverIP, serverPort, 120000, withSSL)
        {

        }

        public CommunicationProtocol SendAndReceive(string method, byte[] body, string session, string contentType)
        {
            if(withSSL)
            {
                return SendAndReceiveSSL(method, body, session, contentType);
            }

            //create and connect socket
            Socket client = new Socket(serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = receiveTimeout;
            client.Connect(serverEndPoint);

            //req
            string reqRes = "req";

            //headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Method", method);
            headers.Add("Session", session);
            headers.Add("Content-Type", contentType);
            headers.Add("Content-Length", body.Length + "");

            CommunicationProtocol sentCommunicationProtocol = new CommunicationProtocol(reqRes, headers, body);

            //send message
            client.Send(sentCommunicationProtocol.ToBytes());

            //receive message
            byte[] receivedBytes = ReceiveAsByteArray(client);
            CommunicationProtocol receivedCommunicationProtocol = CommunicationProtocol.FromBytes(receivedBytes);

            return receivedCommunicationProtocol;
        }

        public CommunicationProtocol SendAndReceive(string method, byte[] body, string session)
        {
            if(withSSL)
            {
                return SendAndReceiveSSL(method, body, session);
            }

            //create and connect socket
            Socket client = new Socket(serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = receiveTimeout;
            client.Connect(serverEndPoint);

            //req
            string reqRes = "req";

            //headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Method", method);
            headers.Add("Session", session);
            headers.Add("Content-Length", body.Length + "");

            CommunicationProtocol sentCommunicationProtocol = new CommunicationProtocol(reqRes, headers, body);

            //send message
            client.Send(sentCommunicationProtocol.ToBytes());

            //receive message
            byte[] receivedBytes = ReceiveAsByteArray(client);
            CommunicationProtocol receivedCommunicationProtocol = CommunicationProtocol.FromBytes(receivedBytes);

            return receivedCommunicationProtocol;
        }

        private byte[] ReceiveAsByteArray(Socket client)
        {
            //receive req, res
            byte[] reqResBytes = new byte[3];
            int reqResReceived = client.Receive(reqResBytes);
            if (reqResReceived != reqResBytes.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            //receive header length
            byte[] headerLengthBytes = new byte[6];
            int headerLengthReceived = client.Receive(headerLengthBytes);
            if (headerLengthReceived != headerLengthBytes.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(headerLengthBytes);
            }
            Int32 headerLength = BitConverter.ToInt32(headerLengthBytes, 1);

            //receive headers
            byte[] headers = new byte[headerLength-9];
            int headersReceived = client.Receive(headers);
            if(headersReceived != headers.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            //get Content-Length header
            string headersStr = Encoding.ASCII.GetString(headers);
            string contentLengthStr = Regex.Match(headersStr, @"Content-Length=[0-9]+").Value;
            int contentLength = int.Parse(contentLengthStr.Split("=")[1]);

            //receive body
            byte[] body = new byte[contentLength];
            int bodyReceived = client.Receive(body);
            if (bodyReceived != body.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            byte[] byteArr = new byte[headerLength + contentLength];
            int byteIdx = 0;

            //req res
            for (int i = 0; i < reqResBytes.Length; i++) byteArr[byteIdx++] = reqResBytes[i];

            //header length
            for (int i = 0; i < headerLengthBytes.Length; i++) byteArr[byteIdx++] = headerLengthBytes[i];

            //headers
            for (int i = 0; i < headers.Length; i++) byteArr[byteIdx++] = headers[i];

            //body
            for (int i = 0; i < body.Length; i++) byteArr[byteIdx++] = body[i];

            return byteArr;
        }

        private CommunicationProtocol SendAndReceiveSSL(string method, byte[] body, string session)
        {
            //create and connect socket
            Socket client = new Socket(serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = receiveTimeout;
            client.Connect(serverEndPoint);

            //create ssl stream
            NetworkStream networkStream = new NetworkStream(client, ownsSocket: true);
            SslStream sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            //authenticate the server
            sslStream.AuthenticateAsClient("localhost");

            //req
            string reqRes = "req";

            //headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Method", method);
            headers.Add("Session", session);
            headers.Add("Content-Length", body.Length + "");

            CommunicationProtocol sentCommunicationProtocol = new CommunicationProtocol(reqRes, headers, body);

            //send message
            sslStream.Write(sentCommunicationProtocol.ToBytes());
            sslStream.Flush();

            //receive message
            byte[] receivedBytes = ReceiveAsByteArraySSL(sslStream);
            CommunicationProtocol receivedCommunicationProtocol = CommunicationProtocol.FromBytes(receivedBytes);

            return receivedCommunicationProtocol;
        }

        private CommunicationProtocol SendAndReceiveSSL(string method, byte[] body, string session, string contentType)
        {
            //create and connect socket
            Socket client = new Socket(serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = receiveTimeout;
            client.Connect(serverEndPoint);

            //create ssl stream
            NetworkStream networkStream = new NetworkStream(client, ownsSocket: true);
            SslStream sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            //authenticate the server
            sslStream.AuthenticateAsClient(serverIP.ToString());

            //req
            string reqRes = "req";

            //headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Method", method);
            headers.Add("Session", session);
            headers.Add("Content-Type", contentType);
            headers.Add("Content-Length", body.Length + "");

            CommunicationProtocol sentCommunicationProtocol = new CommunicationProtocol(reqRes, headers, body);

            //send message
            sslStream.Write(sentCommunicationProtocol.ToBytes());
            sslStream.Flush();

            //receive message
            byte[] receivedBytes = ReceiveAsByteArraySSL(sslStream);
            CommunicationProtocol receivedCommunicationProtocol = CommunicationProtocol.FromBytes(receivedBytes);

            return receivedCommunicationProtocol;
        }

        private byte[] ReceiveAsByteArraySSL(SslStream sslStream)
        {
            //receive req, res
            byte[] reqResBytes = new byte[3];
            int reqResReceived = sslStream.Read(reqResBytes, 0, reqResBytes.Length);
            if (reqResReceived != reqResBytes.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            //receive header length
            byte[] headerLengthBytes = new byte[6];
            int headerLengthReceived = sslStream.Read(headerLengthBytes, 0, headerLengthBytes.Length);
            if (headerLengthReceived != headerLengthBytes.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(headerLengthBytes);
            }
            Int32 headerLength = BitConverter.ToInt32(headerLengthBytes, 1);

            //receive headers
            byte[] headers = new byte[headerLength - 9];
            int headersReceived = sslStream.Read(headers, 0, headers.Length);
            if (headersReceived != headers.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            //get Content-Length header
            string headersStr = Encoding.ASCII.GetString(headers);
            string contentLengthStr = Regex.Match(headersStr, @"Content-Length=[0-9]+").Value;
            int contentLength = int.Parse(contentLengthStr.Split("=")[1]);

            //receive body
            byte[] body = new byte[contentLength];
            int bodyReceived = sslStream.Read(body, 0, body.Length);
            if (bodyReceived != body.Length)
            {
                throw new Exception(); //TBD deal with this or throw costum exception
            }

            byte[] byteArr = new byte[headerLength + contentLength];
            int byteIdx = 0;

            //req res
            for (int i = 0; i < reqResBytes.Length; i++) byteArr[byteIdx++] = reqResBytes[i];

            //header length
            for (int i = 0; i < headerLengthBytes.Length; i++) byteArr[byteIdx++] = headerLengthBytes[i];

            //headers
            for (int i = 0; i < headers.Length; i++) byteArr[byteIdx++] = headers[i];

            //body
            for (int i = 0; i < body.Length; i++) byteArr[byteIdx++] = body[i];

            return byteArr;
        }

        //currently authentication always returns true
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            //Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            // Do not allow this client to communicate with unauthenticated servers.
            return true;
        }
    }
}
