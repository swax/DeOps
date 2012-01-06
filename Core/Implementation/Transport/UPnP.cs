using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

using DeOps.Implementation.Dht;


namespace DeOps.Implementation.Transport
{
    public class UPnPDevice
    {
        public string Name;
        public string URL;
         
        public string DeviceIP;
        public string LocalIP;
    }

    public class PortEntry
    {
        public UPnPDevice Device;
        public string Description;
        public string Protocol;
        public int Port;

        public override string ToString()
        {
            return Description;
        }
    }

    public enum UpnpLogType { In, Out, Error, Other }

    public class UPnPHandler
    {

        DhtNetwork Network;

        public List<UPnPDevice> Devices = new List<UPnPDevice>();

        public bool Logging;
        public ThreadedList<Tuple<UpnpLogType, string>> Log = new ThreadedList<Tuple<UpnpLogType, string>>();

        bool StopThread;
        public Thread WorkingThread;
        public Queue<Action> ActionQueue = new Queue<Action>();


        public UPnPHandler(DhtNetwork network)
        {
            Network = network;

            Initialize();
        }

        public void Initialize()
        {
            if (Network.Core.Sim != null)
                return;

            ushort tcp = Network.TcpControl.ListenPort;
            ushort udp = Network.UdpControl.ListenPort;

            lock (ActionQueue)
                ActionQueue.Enqueue(() =>
                {
                    RefreshDevices();

                    foreach (UPnPDevice device in Devices) // to array so refs aren't reset
                    {
                        if (StopThread) break;
                        OpenPort(device, "TCP", tcp);

                        if (StopThread) break;
                        OpenPort(device, "UDP", udp);
                    }
                });
        }

        public void Shutdown()
        {
            lock (ActionQueue)
                ActionQueue.Clear();

            StopThread = true;

            if (WorkingThread != null)
                WorkingThread.Join(1000);
        }

        public void SecondTimer()
        {
            if (WorkingThread != null || ActionQueue.Count == 0)
                return;

            Debug.Assert(WorkingThread == null);

            WorkingThread = new Thread(() =>
            {
                Action next = null;

                while (!StopThread && ActionQueue.Count > 0)
                {
                    lock (ActionQueue)
                        next = ActionQueue.Dequeue();

                    try
                    {
                        next.Invoke();
                    }
                    catch (Exception ex)
                    {
                        UpdateLog(UpnpLogType.Error, ex.Message);
                    }
                }

                WorkingThread = null;
            });
            WorkingThread.Name = "UPnP Thread";

            WorkingThread.Start();
        }

        public void ClosePorts()
        {
            if (Network.Core.Sim != null)
                return;

            ushort tcp = Network.TcpControl.ListenPort;
            ushort udp = Network.UdpControl.ListenPort;

            lock (ActionQueue)
                ActionQueue.Enqueue(() =>
                {
                    foreach (UPnPDevice device in Devices) // to array so refs aren't reset
                    {
                        if (StopThread) break;
                        ClosePort(device, "TCP", tcp);

                        if (StopThread) break;
                        ClosePort(device, "UDP", udp);
                    }
                });
        }

        public void RefreshDevices()
        {
            Devices.Clear();


            //for each nic in computer...
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    string machineIP = null;

                    // on vista the first address is IPv6 - routers dont know how to deal with that
                    foreach (UnicastIPAddressInformation info in nic.GetIPProperties().UnicastAddresses)
                        if (info.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            machineIP = info.Address.ToString();
                            break;
                        }

                    if (machineIP == null)
                        continue;

                    //send msg to each gateway configured on this nic
                    foreach (GatewayIPAddressInformation gwInfo in nic.GetIPProperties().GatewayAddresses)
                    {
                        string firewallIP = gwInfo.Address.ToString();

                        UpdateLog(UpnpLogType.Other, "Local IP: " + machineIP + ", Gateway IP: " + firewallIP);

                        QueryDevices(machineIP, firewallIP);
                    }
                }
                catch (Exception ex)
                {
                    UpdateLog(UpnpLogType.Error, ex.Message);
                }
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
                                "MX:1\r\n" +
                                "\r\n";

                UpdateLog(UpnpLogType.Out, query);

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
            catch (Exception ex)
            {
                UpdateLog(UpnpLogType.Error, ex.Message);
            }

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

            UpdateLog(UpnpLogType.In, queryResponse);

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
                UpdateLog(UpnpLogType.Out, "Requesting: " + location);

                xml = Utilities.WebDownloadString(location);

                UpdateLog(UpnpLogType.In, xml);
            }
            catch (System.Exception ex)
            {
                UpdateLog(UpnpLogType.Error, ex.Message);
            }

            TryAddDevice(xml, "WANIPConnection", machineIP, firewallIP);
            TryAddDevice(xml, "WANPPPConnection", machineIP, firewallIP);
        }

        private void UpdateLog(UpnpLogType type, string message)
        {
            if (type == UpnpLogType.Other || type == UpnpLogType.Error)
                Network.UpdateLog("UPnP", message);

            if (!Logging)
                return;

            if (type == UpnpLogType.In || type == UpnpLogType.Out)
                message = FormatXml(message);

            Log.SafeAdd(new Tuple<UpnpLogType, string>(type, message));
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

            UpdateLog(UpnpLogType.Other, "Device Added: " + url);
        }

        public void OpenDefaultPorts()
        {
            foreach (UPnPDevice device in Devices)
            {
                OpenPort(device, "TCP", Network.TcpControl.ListenPort);
                OpenPort(device, "UDP", Network.UdpControl.ListenPort);
            }
            // in thread check for cancel (when core closing)

            // in dispose, join the thread
        }

        public void OpenPort(UPnPDevice device, string protocol, int port)
        {
            UpdateLog(UpnpLogType.Other, "Opening " + protocol + " Port " + port + " on " + device.Name);

            string description = "DeOps - ";

            if (Network.Core.User != null)
                description += Network.Core.User.Settings.Operation + " - " + Network.Core.User.Settings.UserName;
            else
                description += "Lookup";
                
            string body =  @"<u:AddPortMapping xmlns:u=""urn:schemas-upnp-org:service:<?=DeviceName?>:1"">
                            <NewRemoteHost></NewRemoteHost>
                            <NewExternalPort><?=NewExternalPort?></NewExternalPort>
                            <NewProtocol><?=NewProtocol?></NewProtocol>
                            <NewInternalPort><?=NewInternalPort?></NewInternalPort>
                            <NewInternalClient><?=NewInternalClient?></NewInternalClient>
                            <NewEnabled>1</NewEnabled>
                            <NewPortMappingDescription><?=NewPortMappingDescription?></NewPortMappingDescription>
                            <NewLeaseDuration>0</NewLeaseDuration>
                            </u:AddPortMapping>";

            body = body.Replace("<?=DeviceName?>", device.Name);
            body = body.Replace("<?=NewExternalPort?>", port.ToString());
            body = body.Replace("<?=NewProtocol?>", protocol);
            body = body.Replace("<?=NewInternalPort?>", port.ToString());
            body = body.Replace("<?=NewInternalClient?>", device.LocalIP);
            body = body.Replace("<?=NewPortMappingDescription?>", description);

            string ret = PerformAction(device, "AddPortMapping", body);
        }

        public void ClosePort(UPnPDevice device, string protocol, int port)
        {
            UpdateLog(UpnpLogType.Other, "Closing " + protocol + " Port " + port + " on " + device.Name);

            string body = @"<u:DeletePortMapping xmlns:u=""urn:schemas-upnp-org:service:<?=DeviceName?>:1"">
                            <NewRemoteHost></NewRemoteHost>
                            <NewExternalPort><?=NewExternalPort?></NewExternalPort>
                            <NewProtocol><?=NewProtocol?></NewProtocol>
                            </u:DeletePortMapping>";

            body = body.Replace("<?=DeviceName?>", device.Name);
            body = body.Replace("<?=NewExternalPort?>", port.ToString());
            body = body.Replace("<?=NewProtocol?>", protocol);

            string ret = PerformAction(device, "DeletePortMapping", body);
        }


        public PortEntry GetPortEntry(UPnPDevice device, int index)
        {
            UpdateLog(UpnpLogType.Other, "Getting port map index " + index + " on " + device.Name);

            try
            {
            string body = @"<u:GetGenericPortMappingEntry xmlns:u=""urn:schemas-upnp-org:service:<?=DeviceName?>:1"">
                           <NewPortMappingIndex><?=NewPortMappingIndex?></NewPortMappingIndex>
                           </u:GetGenericPortMappingEntry>";

                body = body.Replace("<?=DeviceName?>", device.Name);
                body = body.Replace("<?=NewPortMappingIndex?>", index.ToString());

                string ret = PerformAction(device, "GetGenericPortMappingEntry", body);

                if (ret == null || ret == "")
                    return null;

                string name = ExtractTag("NewPortMappingDescription", ret);
                string ip = ExtractTag("NewInternalClient", ret);
                string port = ExtractTag("NewInternalPort", ret);
                string protocol = ExtractTag("NewProtocol", ret);

                PortEntry entry = new PortEntry();
                entry.Device = device;
                entry.Description = index + ": " + name + " - " + ip + ":" + port + " " + protocol;
                entry.Port = int.Parse(port);
                entry.Protocol = protocol;

                return entry;
            }
            catch (Exception ex)
            {
                UpdateLog(UpnpLogType.Error, ex.Message);
            }

            return null;
        }

        string PerformAction(UPnPDevice device, string action, string soap)
        {
            try
            {
                string soapBody =  @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/ "" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/ "">
                                    <s:Body>
                                    <?=soap?>
                                    </s:Body>
                                    </s:Envelope>";

                soapBody = soapBody.Replace("<?=soap?>", soap);

                UpdateLog(UpnpLogType.Out, soapBody);

                byte[] body = UTF8Encoding.ASCII.GetBytes(soapBody);


                WebRequest wr = WebRequest.Create(device.URL);
                wr.Timeout = 5000;
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
                string xml = sr.ReadToEnd();
                sr.Close();

                UpdateLog(UpnpLogType.In, xml);

                return xml;
            }
            catch (Exception ex)
            {
                UpdateLog(UpnpLogType.Error, ex.Message);
            }

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

        private string FormatXml(string unformatted)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(unformatted);
            }
            catch
            {
                return unformatted;
            }

            StringBuilder final = new StringBuilder();

            using (StringWriter stream = new StringWriter(final))
            {
                XmlTextWriter xml = null;

                try
                {
                    xml = new XmlTextWriter(stream);
                    xml.Formatting = Formatting.Indented;
                    doc.WriteTo(xml);
                }
                finally
                {
                    if (xml != null)
                        xml.Close();
                }
            }

            return final.ToString();
        }


    }
}