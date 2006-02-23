using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net.NetworkInformation;
using System.Net;

namespace BlueprintIT.Audit
{
  internal class NetworkAuditor : Auditor
  {
    public override string ID
    {
      get { return "NetworkAudit"; }
    }

    public override void Audit(XmlElement el)
    {
      XmlElement element = el.OwnerDocument.CreateElement(ID);
      el.AppendChild(element);

      foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces())
      {
        if (iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        {
          XmlElement ifel = element.OwnerDocument.CreateElement("Interface");
          element.AppendChild(ifel);
          ifel.SetAttribute("id", iface.Id);
          ifel.SetAttribute("name", iface.Name);
          ifel.SetAttribute("type", iface.NetworkInterfaceType.ToString().ToLower());
          ifel.SetAttribute("status", iface.OperationalStatus.ToString().ToLower());
          ifel.SetAttribute("mac", iface.GetPhysicalAddress().ToString().ToUpper());
          
          XmlElement tcpip = element.OwnerDocument.CreateElement("TCPIP");
          ifel.AppendChild(tcpip);
          tcpip.SetAttribute("id", "0");
          IPInterfaceProperties ipprops = iface.GetIPProperties();
          tcpip.SetAttribute("suffix", ipprops.DnsSuffix);
          foreach (IPAddress address in ipprops.DnsAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("DnsAddress");
            addrel.SetAttribute("address", address.ToString());
            tcpip.AppendChild(addrel);
          }
          foreach (IPAddress address in ipprops.DhcpServerAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("DhcpAddress");
            addrel.SetAttribute("address", address.ToString());
            tcpip.AppendChild(addrel);
          }
          foreach (GatewayIPAddressInformation address in ipprops.GatewayAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("GatewayAddress");
            addrel.SetAttribute("address", address.Address.ToString());
            tcpip.AppendChild(addrel);
          }
          foreach (UnicastIPAddressInformation address in ipprops.UnicastAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("IPAddress");
            addrel.SetAttribute("address", address.Address.ToString());
            tcpip.AppendChild(addrel);
          }
        }
      }
    }
  }
}
