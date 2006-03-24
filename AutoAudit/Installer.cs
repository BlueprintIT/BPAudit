using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace BlueprintIT.Audit.AutoAudit
{
  [RunInstaller(true)]
  public partial class AutoAuditInstaller : Installer
  {
    public AutoAuditInstaller()
    {
      InitializeComponent();

      ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
      ServiceInstaller serviceInstaller = new ServiceInstaller();

      processInstaller.Account = ServiceAccount.LocalSystem;
      serviceInstaller.StartType = ServiceStartMode.Automatic;
      serviceInstaller.ServiceName = "Blueprint IT Auditor";
      serviceInstaller.Description = "Performs routine auditing of the computer's systems to notify Blueprint IT of any irregularities before they become problems.";

      Installers.Add(processInstaller);
      Installers.Add(serviceInstaller);
    }
  }
}