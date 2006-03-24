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
        pel.SetAttribute("memory", process.WorkingSet64.ToString());
        try
        {
          pel.SetAttribute("cpu", process.TotalProcessorTime.TotalSeconds.ToString("F0"));
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
