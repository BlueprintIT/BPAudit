using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using BlueprintIT.Audit;
using System.Diagnostics;
using System.Text;
using System.Net;

namespace BlueprintIT.Audit.AuditTool
{
  public partial class AuditTool : Form
  {
    public AuditTool()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      AuditManager.AuditAndSubmit();
    }
  }
}