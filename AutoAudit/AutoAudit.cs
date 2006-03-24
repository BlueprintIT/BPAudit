using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using BlueprintIT.Audit;

namespace AutoAudit
{
  public partial class AutoAudit : ServiceBase
  {
    public AutoAudit()
    {
      InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
      AuditManager.StartMonitors();
    }

    protected override void OnPause()
    {
      OnStop();
    }

    protected override void OnContinue()
    {
      OnStart(null);
    }
    
    protected override void OnStop()
    {
      AuditManager.StopMonitors();
    }

    protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
    {
      return true;
    }

    protected override void OnSessionChange(SessionChangeDescription changeDescription)
    {
    }

    protected override void OnShutdown()
    {
    }
  }
}
