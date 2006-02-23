using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace BlueprintIT.Audit
{
  internal class DiskAuditor : Auditor
  {
    public override string ID
    {
      get { return "DiskAudit"; }
    }

    public override void Audit(XmlElement el)
    {
      XmlElement element = el.OwnerDocument.CreateElement(ID);
      el.AppendChild(element);

      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        XmlElement drv = element.OwnerDocument.CreateElement("Disk");
        element.AppendChild(drv);
        drv.SetAttribute("id", drive.Name[0].ToString());
        drv.SetAttribute("path", drive.Name);
        drv.SetAttribute("type", drive.DriveType.ToString().ToLower());
        if (drive.DriveType == DriveType.Fixed)
        {
          drv.SetAttribute("totalsize", drive.TotalSize.ToString());
          drv.SetAttribute("freespace", drive.AvailableFreeSpace.ToString());
          drv.SetAttribute("label", drive.VolumeLabel);
          drv.SetAttribute("filesystem", drive.DriveFormat.ToLower());
        }
      }
    }
  }
}
