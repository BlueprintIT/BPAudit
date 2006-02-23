using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.ServiceProcess;

namespace BlueprintIT.Audit
{
  internal class ServiceAuditor : Auditor
  {
    public override string ID
    {
      get { return "ServiceAudit"; }
    }

    public override void Audit(XmlElement el)
    {
      XmlElement element = el.OwnerDocument.CreateElement(ID);
      el.AppendChild(element);

      foreach (ServiceController service in ServiceController.GetServices())
      {
        XmlElement sel = element.OwnerDocument.CreateElement("Service");
        element.AppendChild(sel);
        sel.SetAttribute("id", service.ServiceName);
        sel.SetAttribute("name", service.DisplayName);
        sel.SetAttribute("status", service.Status.ToString().ToLower());
      }

      foreach (ServiceController service in ServiceController.GetDevices())
      {
        XmlElement sel = element.OwnerDocument.CreateElement("Driver");
        element.AppendChild(sel);
        sel.SetAttribute("id", service.ServiceName);
        sel.SetAttribute("name", service.DisplayName);
        sel.SetAttribute("status", service.Status.ToString().ToLower());
      }
    }
  }
}
