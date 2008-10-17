using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;



namespace RiseOp.Implementation.Transport
{
    //crit implement / make asyncronous yet / need to periodically advertise? (rate probably in response) / clean up on close

    public class UPnP
    {
        public UPnP()
        {

        }

        public static void OpenFirewallPort(int port)
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            //for each nic in computer...
            foreach (NetworkInterface nic in nics)
            {
                try
                {
                    string machineIP = nic.GetIPProperties().UnicastAddresses[0].Address.ToString();

                    //send msg to each gateway configured on this nic
                    foreach(GatewayIPAddressInformation gwInfo in nic.GetIPProperties().GatewayAddresses)
                    {
                        try
                        {
                            OpenFirewallPort(machineIP, gwInfo.Address.ToString(), port);
                        }
                        catch
                        { }
                    }
                }
                catch { }
            }

        }

        public static void OpenFirewallPort(string machineIP, string firewallIP, int openPort)
        {
            string svc = GetServicesFromDevice(firewallIP);

            string url = ExtractTag("URLBase", svc);
            if (url == null)
                url = "http://" + machineIP + ":80";


            //test(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", url);
            test(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", url);

            OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", machineIP, url, openPort, "TCP");
            OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", machineIP, url, openPort, "UDP");

            OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", machineIP, url, openPort, "TCP");
            OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", machineIP, url, openPort, "UDP");
        }


        private static string GetServicesFromDevice(string firewallIP)
        {
            //To send a broadcast and get responses from all, send to 239.255.255.250
            string queryResponse = "";
            try
            {
                string query =  "M-SEARCH * HTTP/1.1\r\n" +
                                "Host:" + firewallIP + ":1900\r\n" +
                                "ST:upnp:rootdevice\r\n" +
                                "Man:\"ssdp:discover\"\r\n" +
                                "MX:3\r\n" +
                                "\r\n" +
                                "\r\n";

                //use sockets instead of UdpClient so we can set a timeout easier
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(firewallIP), 1900);

                //1.5 second timeout because firewall should be on same segment (fast)
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

                byte[] q = Encoding.ASCII.GetBytes(query);
                client.SendTo(q, q.Length, SocketFlags.None, endPoint);
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint senderEP = (EndPoint)sender;

                byte[] data = new byte[4096];
                int recv = client.ReceiveFrom(data, ref senderEP);
                queryResponse = Encoding.ASCII.GetString(data);
            }
            catch { }

            if (queryResponse.Length == 0)
                return "";


            /* QueryResult is somthing like this:
            *
            HTTP/1.1 200 OK
            Cache-Control:max-age=60
            Location:http://10.10.10.1:80/upnp/service/des_ppp.xml
            Server:NT/5.0 UPnP/1.0
            ST:upnp:rootdevice
            EXT:

            USN:uuid:upnp-InternetGatewayDevice-1_0-00095bd945a2::upnp:rootdevice
            */

            string location = "";
            string[] parts = queryResponse.Split(new string[] {System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.ToLower().StartsWith("location"))
                {
                    location = part.Substring(part.IndexOf(':') + 1);
                    break;
                }
            }
            if (location.Length == 0)
                return "";

            //then using the location url, we get more information:

            try
            {
                string ret = Utilities.WebDownloadString(location);
                Debug.WriteLine(ret);
                return ret;//return services
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return "";
        }

        private static void OpenPortFromService(string services, string serviceType, string machineIP, string url, int portToForward, string protocol)
        {
            string body =   "<u:AddPortMapping xmlns:u=\"" + serviceType + "\">" +
                            "<NewRemoteHost></NewRemoteHost>" +
                            "<NewExternalPort>" + portToForward.ToString() + "</NewExternalPort>" +
                            "<NewProtocol>" + protocol + "</NewProtocol>" +
                            "<NewInternalPort>" + portToForward.ToString() + "</NewInternalPort>" +
                            "<NewInternalClient>" + machineIP + "</NewInternalClient>" +
                            "<NewEnabled>1</NewEnabled>" +
                            "<NewPortMappingDescription>WoodchopClient</NewPortMappingDescription>" +
                            "<NewLeaseDuration>0</NewLeaseDuration>" +
                            "</u:AddPortMapping>";

            string ret = PerformAction(services, serviceType, url, "AddPortMapping", body);

            Debug.WriteLine("Setting port forwarding:" + portToForward.ToString() + "\r\r" + ret);
        }



        static void  test(string services, string serviceType, string url)
        {
            for (int i = 0; i < 100; i++)
            {
                string body = "<u:GetGenericPortMappingEntry xmlns:u=\"" + serviceType + "\">" +
                              "<NewPortMappingIndex>" + i + "</NewPortMappingIndex>" +
                              "</u:GetGenericPortMappingEntry>";

                string ret = PerformAction(services, serviceType, url, "GetGenericPortMappingEntry", body);

                string name = ExtractTag("NewPortMappingDescription", ret);
                string ip = ExtractTag("NewInternalClient", ret);
                string port = ExtractTag("NewInternalPort", ret);
                string protocol = ExtractTag("NewProtocol", ret);
                
                Debug.WriteLine(i + ": " + name + " - " + ip + ":" + port + " " + protocol);
            }
        }

        static string PerformAction(string services, string serviceType, string url, string action, string soap)
        {
            try
            {
                if (services.Length == 0)
                    return null;

                int svcIndex = services.IndexOf(serviceType);
                if (svcIndex == -1)
                    return null;

                string controlUrl = ExtractTag("controlURL", services.Substring(svcIndex));


                string soapBody = "<s:Envelope " +
                                    "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" " +
                                    "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" +
                                    "<s:Body>" +
                                    soap +
                                    "</s:Body>" +
                                    "</s:Envelope>";

                byte[] body = UTF8Encoding.ASCII.GetBytes(soapBody);

                url += controlUrl;

                WebRequest wr = WebRequest.Create(url);

                wr.Method = "POST";
                wr.Headers.Add("SOAPAction", "\"" + serviceType + "#" + action + "\"");
                wr.ContentType = "text/xml;charset=\"utf-8\"";
                wr.ContentLength = body.Length;

                Stream stream = wr.GetRequestStream();
                stream.Write(body, 0, body.Length);
                stream.Flush();
                stream.Close();

                WebResponse wres = wr.GetResponse();
                StreamReader sr = new StreamReader(wres.GetResponseStream());
                string ret = sr.ReadToEnd();
                sr.Close();

                return ret;
            }
            catch { }

            return null;
        }

        private static string ExtractTag(string tag, string body)
        {
            try
            {
                string tag1 = "<" + tag + ">";
                string tag2 = "</" + tag + ">";

                string extracted = body;
                extracted = extracted.Substring(extracted.IndexOf(tag1) + tag1.Length);
                extracted = extracted.Substring(0, extracted.IndexOf(tag2));

                return extracted;
            }
            catch { }

            return null;
        }
    }
}