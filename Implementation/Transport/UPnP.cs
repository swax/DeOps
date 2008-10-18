using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

using RiseOp.Implementation.Dht;


namespace RiseOp.Implementation.Transport
{
    //crit implement / make asyncronous yet / need to periodically advertise? (rate probably in response) / clean up on close

    internal class UPnPDevice
    {
        internal string Name;
        internal string URL;

        internal string DeviceIP;
        internal string LocalIP;
    }

    internal class PortEntry
    {
        internal string Description;
        internal string Protocol;
        internal int Port;

        public override string ToString()
        {
            return Description;
        }
    }


    internal class UPnPHandler
    {
        internal List<UPnPDevice> Devices = new List<UPnPDevice>();

        DhtNetwork Network;


        internal UPnPHandler(DhtNetwork network)
        {
            Network = network;
        }

        internal void RefreshDevices()
        {
            Devices.Clear();


            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            //for each nic in computer...
            foreach (NetworkInterface nic in nics)
            {
                try
                {
                    var unicasts = nic.GetIPProperties().UnicastAddresses;
                    if (unicasts.Count == 0)
                        continue;

                    string machineIP = unicsasts[0].Address.ToString();

                    //send msg to each gateway configured on this nic
                    foreach (GatewayIPAddressInformation gwInfo in nic.GetIPProperties().GatewayAddresses)
                    {
                        string firewallIP = gwInfo.Address.ToString();

                        QueryDevices(machineIP, firewallIP);
                    }
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("UPnP", "RefreshGateways1:" + ex.Message);
                }
            }
        }

        internal void GetOpenPorts()
        {
            foreach (UPnPDevice device in Devices)
            {
                for (int i = 0; i < 200; i++)
                    GetPortEntry(device, i);
            }
        }

        internal void OpenFirewallPort(int port)
        {
           
            foreach (UPnPDevice device in Devices)
            {
                OpenPort(device, "TCP", port);
                OpenPort(device, "UDP", port);
            }
        }

        private void QueryDevices(string machineIP, string firewallIP)
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
                return;


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
                return;

            //then using the location url, we get more information:

            string xml = "";

            try
            {
                xml = Utilities.WebDownloadString(location);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            TryAddDevice(xml, "WANIPConnection", machineIP, firewallIP);
            TryAddDevice(xml, "WANPPPConnection", machineIP, firewallIP);
        }

        void TryAddDevice(string xml, string name, string machineIP, string firewallIP)
        {
            string url = ExtractTag("URLBase", xml);
            if (url == null)
                url = "http://" + firewallIP + ":80";

            int pos = xml.IndexOf("urn:schemas-upnp-org:service:" + name + ":1");
            if (pos == -1)
                return;

            string control = ExtractTag("controlURL", xml.Substring(pos));
            if (control == null || control == "")
                return;

            url += control;

            Devices.Add(new UPnPDevice()
            {
                Name = name,
                DeviceIP = firewallIP,
                LocalIP = machineIP,
                URL = url
            });
        }

        void OpenPort(UPnPDevice device, string protocol, int port)
        {
            string body = "<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + device.Name + ":1\">" +
                            "<NewRemoteHost></NewRemoteHost>" +
                            "<NewExternalPort>" + port.ToString() + "</NewExternalPort>" +
                            "<NewProtocol>" + protocol + "</NewProtocol>" +
                            "<NewInternalPort>" + port.ToString() + "</NewInternalPort>" +
                            "<NewInternalClient>" + device.LocalIP + "</NewInternalClient>" +
                            "<NewEnabled>1</NewEnabled>" +
                            "<NewPortMappingDescription>RiseOp</NewPortMappingDescription>" +
                            "<NewLeaseDuration>0</NewLeaseDuration>" +
                            "</u:AddPortMapping>";

            string ret = PerformAction(device, "AddPortMapping", body);
        }

        void ClosePort(UPnPDevice device, string protocol, int port)
        {
            string body = "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + device.Name + ":1\">" +
                            "<NewRemoteHost></NewRemoteHost>" +
                            "<NewExternalPort>" + port.ToString() + "</NewExternalPort>" +
                            "<NewProtocol>" + protocol + "</NewProtocol>" +
                            "</u:DeletePortMapping>";

            string ret = PerformAction(device, "DeletePortMapping", body);
        }


        internal PortEntry GetPortEntry(UPnPDevice device, int index)
        {
            try
            {
                string body = "<u:GetGenericPortMappingEntry xmlns:u=\"urn:schemas-upnp-org:service:" + device.Name + ":1\">" +
                              "<NewPortMappingIndex>" + index + "</NewPortMappingIndex>" +
                              "</u:GetGenericPortMappingEntry>";

                string ret = PerformAction(device, "GetGenericPortMappingEntry", body);

                if (ret == null || ret == "")
                    return null;

                string name = ExtractTag("NewPortMappingDescription", ret);
                string ip = ExtractTag("NewInternalClient", ret);
                string port = ExtractTag("NewInternalPort", ret);
                string protocol = ExtractTag("NewProtocol", ret);

                PortEntry entry = new PortEntry();
                entry.Description = index + ": " + name + " - " + ip + ":" + port + " " + protocol;
                entry.Port = int.Parse(port);
                entry.Protocol = protocol;

                return entry;
            }
            catch { }

            return null;
        }

        static string PerformAction(UPnPDevice device, string action, string soap)
        {
            try
            {
                string soapBody = "<s:Envelope " +
                                    "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" " +
                                    "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" +
                                    "<s:Body>" +
                                    soap +
                                    "</s:Body>" +
                                    "</s:Envelope>";

                byte[] body = UTF8Encoding.ASCII.GetBytes(soapBody);


                WebRequest wr = WebRequest.Create(device.URL);

                wr.Method = "POST";
                wr.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:" + device.Name + ":1#" + action + "\"");
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