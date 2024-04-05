using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManagerClient
{
    class PMClientException : Exception
    {
        public SocketException SE;
        public string Details;
        public string Reason;

        public PMClientException(SocketException e)
        {
            SE = e;

            if(e.NativeErrorCode == 10061)
            {
                Reason = "Connection Refused";
                Details = "Connection refused.\r\nNo connection could be made because the target computer actively refused it. This usually results from trying to connect to a service that is inactive on the foreign host—that is, one with no server application running.";
            }
            else if(e.NativeErrorCode == 10060)
            {
                Reason = "Connection Timeout";
                Details = "Connection timed out.\r\nA connection attempt failed because the connected party did not properly respond after a period of time, or the established connection failed because the connected host has failed to respond.";
            }
        }
    }
}
