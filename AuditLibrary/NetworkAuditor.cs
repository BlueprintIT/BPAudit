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
    public string ID
    {
      get { return "network"; }
    }

    public string[] Component
    {
      get { return new string[] { ID }; }
    }

    public void Audit(XmlElement element)
    {
      foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces())
      {
        if (iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        {
          XmlElement ifel = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
          element.AppendChild(ifel);
          ifel.SetAttribute("id", iface.Id);
          ifel.SetAttribute("name", iface.Name);
          ifel.SetAttribute("type", iface.NetworkInterfaceType.ToString().ToLower());
          ifel.SetAttribute("status", iface.OperationalStatus.ToString().ToLower());
          ifel.SetAttribute("mac", iface.GetPhysicalAddress().ToString().ToUpper());

          XmlElement tcpip = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
          ifel.AppendChild(tcpip);
          tcpip.SetAttribute("id", "tcpip");
          IPInterfaceProperties ipprops = iface.GetIPProperties();
          tcpip.SetAttribute("suffix", ipprops.DnsSuffix);

          XmlElement list = element.OwnerDocument.CreateElement("list", AuditManager.AUDIT_NS);
          list.SetAttribute("id", "dns");
          tcpip.AppendChild(list);
          foreach (IPAddress address in ipprops.DnsAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("item", AuditManager.AUDIT_NS);
            addrel.SetAttribute("value", address.ToString());
            list.AppendChild(addrel);
          }

          list = element.OwnerDocument.CreateElement("list", AuditManager.AUDIT_NS);
          list.SetAttribute("id", "dhcp");
          tcpip.AppendChild(list);
          foreach (IPAddress address in ipprops.DhcpServerAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("item", AuditManager.AUDIT_NS);
            addrel.SetAttribute("value", address.ToString());
            list.AppendChild(addrel);
          }

          list = element.OwnerDocument.CreateElement("list", AuditManager.AUDIT_NS);
          list.SetAttribute("id", "gateway");
          tcpip.AppendChild(list);
          foreach (GatewayIPAddressInformation address in ipprops.GatewayAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("item", AuditManager.AUDIT_NS);
            addrel.SetAttribute("value", address.Address.ToString());
            list.AppendChild(addrel);
          }

          list = element.OwnerDocument.CreateElement("list", AuditManager.AUDIT_NS);
          list.SetAttribute("id", "ipaddress");
          tcpip.AppendChild(list);
          foreach (UnicastIPAddressInformation address in ipprops.UnicastAddresses)
          {
            XmlElement addrel = element.OwnerDocument.CreateElement("item", AuditManager.AUDIT_NS);
            addrel.SetAttribute("value", address.Address.ToString());
            list.AppendChild(addrel);
          }
        }
      }
    }
  }
}
