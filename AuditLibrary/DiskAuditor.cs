using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace BlueprintIT.Audit
{
  internal class DiskAuditor : Auditor
  {
    public string ID
    {
      get { return "storage"; }
    }

    public string[] Component
    {
      get { return new string[] { ID }; }
    }

    public void Audit(XmlElement element)
    {
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        XmlElement drv = element.OwnerDocument.CreateElement("component", AuditManager.AUDIT_NS);
        element.AppendChild(drv);
        drv.SetAttribute("id", drive.Name[0].ToString());
        drv.SetAttribute("path", drive.Name);
        drv.SetAttribute("type", drive.DriveType.ToString().ToLower());
        if (drive.DriveType == DriveType.Fixed)
        {
          XmlElement value = element.OwnerDocument.CreateElement("value", AuditManager.AUDIT_NS);
          value.SetAttribute("type", "number");
          value.SetAttribute("id", "totalsize");
          value.SetAttribute("value", drive.TotalSize.ToString());
          drv.AppendChild(value);

          value = element.OwnerDocument.CreateElement("value", AuditManager.AUDIT_NS);
          value.SetAttribute("type", "number");
          value.SetAttribute("id", "freespace");
          value.SetAttribute("value", drive.AvailableFreeSpace.ToString());
          drv.AppendChild(value);

          value = element.OwnerDocument.CreateElement("value", AuditManager.AUDIT_NS);
          value.SetAttribute("type", "number");
          value.SetAttribute("id", "usage");
          value.SetAttribute("value", (drive.TotalSize-drive.TotalFreeSpace).ToString());
          drv.AppendChild(value);

          drv.SetAttribute("label", drive.VolumeLabel);
          drv.SetAttribute("filesystem", drive.DriveFormat.ToLower());
        }
      }
    }
  }
}
