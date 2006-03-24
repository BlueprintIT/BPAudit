using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.ServiceProcess;

namespace BlueprintIT.Audit
{
  internal class ServiceAuditor : Auditor
  {
    public string ID
    {
      get { return "services"; }
    }

    public string[] Component
    {
      get { return new string[] { ID }; }
    }

    public void Audit(XmlElement element)
    {
      XmlElement services = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
      services.SetAttribute("id", "services");
      element.AppendChild(services);
      foreach (ServiceController service in ServiceController.GetServices())
      {
        XmlElement sel = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
        services.AppendChild(sel);
        sel.SetAttribute("id", service.ServiceName);
        sel.SetAttribute("name", service.DisplayName);
        sel.SetAttribute("status", service.Status.ToString().ToLower());
      }

      services = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
      services.SetAttribute("id", "devices");
      element.AppendChild(services);
      foreach (ServiceController service in ServiceController.GetDevices())
      {
        XmlElement sel = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
        services.AppendChild(sel);
        sel.SetAttribute("id", service.ServiceName);
        sel.SetAttribute("name", service.DisplayName);
        sel.SetAttribute("status", service.Status.ToString().ToLower());
      }
    }
  }
}
