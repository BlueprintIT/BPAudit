using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace BlueprintIT.Audit
{
  internal class ProcessAuditor : Auditor
  {
    public string ID
    {
      get { return "processes"; }
    }

    public string[] Component
    {
      get { return new string[] { ID }; }
    }

    public void Audit(XmlElement element)
    {
      foreach (Process process in Process.GetProcesses())
      {
        XmlElement pel = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
        element.AppendChild(pel);
        pel.SetAttribute("id", process.ProcessName);

        XmlElement value = element.OwnerDocument.CreateElement("value", AuditManager.AUDIT_NS);
        value.SetAttribute("type", "number");
        value.SetAttribute("id", "memory");
        value.SetAttribute("value", process.WorkingSet64.ToString());
        pel.AppendChild(value);
        try
        {
          value = element.OwnerDocument.CreateElement("value", AuditManager.AUDIT_NS);
          value.SetAttribute("type", "number");
          value.SetAttribute("id", "cpu");
          value.SetAttribute("value", process.TotalProcessorTime.TotalSeconds.ToString());
          pel.AppendChild(value);
        }
        catch (Exception)
        {
        }
        try
        {
          pel.SetAttribute("module", process.MainModule.FileName);
        }
        catch (Exception)
        {
        }
      }
    }
  }
}
