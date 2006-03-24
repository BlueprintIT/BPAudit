using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Net;
using BlueprintIT.Utils;
using System.Net.NetworkInformation;

namespace BlueprintIT.Audit
{
  public class AuditManager : Auditor
  {
    private static IList<Type> auditors = new List<Type>();
    private static IList<Type> monitors = new List<Type>();
    private static IList<Monitor> monitoring = new List<Monitor>();

    public static readonly string AUDIT_NS = "http://audit.blueprintit.co.uk";

    static AuditManager()
    {
      ScanAssembly(Assembly.GetExecutingAssembly());
      if (Assembly.GetEntryAssembly() != Assembly.GetExecutingAssembly())
        ScanAssembly(Assembly.GetEntryAssembly());

      FileInfo[] configs = DataDir.GetFiles("config.xml");
      if (configs.Length==1)
      {
        config = XmlUtils.LoadXml(configs[0]);
      }
      else
      {
        config = new XmlDocument();
        XmlElement conf = config.CreateElement("config", AUDIT_NS);
        config.AppendChild(conf);
        conf.SetAttribute("version", "0");
        conf.SetAttribute("id", Guid.NewGuid().ToString());
        FlushConfig();
      }
    }

    private static void ScanAssembly(Assembly assembly)
    {
      Type auditor = Type.GetType("BlueprintIT.Audit.Auditor");
      Type monitor = Type.GetType("BlueprintIT.Audit.Monitor");
      if ((auditor != null) && (monitor != null))
      {
        Type[] types = assembly.GetTypes();
        foreach (Type type in types)
        {
          if (type.IsInterface)
            continue;
          if (type.IsAbstract)
            continue;
          if (auditor.IsAssignableFrom(type))
            auditors.Add(type);
          if (monitor.IsAssignableFrom(type))
            monitors.Add(type);
        }
      }
    }

    internal static void FlushConfig()
    {
      string file = DataDir.FullName + "\\config.xml";

      XmlUtils.SaveXml(config, file);
    }

    private static XmlDocument config;
    private static XmlElement Config
    {
      get { return config.DocumentElement; }
    }

    public static XmlElement GetAuditorConfig(Auditor item)
    {
      XmlNode node = Config.FirstChild;
      while (node != null)
      {
        if ((node is XmlElement) && (node.Name == "auditor") && (((XmlElement)node).GetAttribute("id") == item.ID))
          return (XmlElement)node;
      }
      XmlElement element = AuditManager.Config.OwnerDocument.CreateElement("auditor", AUDIT_NS);
      element.SetAttribute("id", item.ID);
      Config.AppendChild(element);
      return element;
    }

    public static XmlElement GetMonitorConfig(Monitor item)
    {
      XmlNode node = Config.FirstChild;
      while (node != null)
      {
        if ((node is XmlElement) && (node.Name == "monitor") && (((XmlElement)node).GetAttribute("id") == item.ID))
          return (XmlElement)node;
      }
      XmlElement element = AuditManager.Config.OwnerDocument.CreateElement("monitor", AUDIT_NS);
      element.SetAttribute("id", item.ID);
      Config.AppendChild(element);
      return element;
    }

    public static DirectoryInfo DataDir
    {
      get
      {
        DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Blueprint IT\\Auditor");
        if (!dir.Exists)
          dir.Create();

        return dir;
      }
    }

    public static XmlDocument GetConfig(string id)
    {
      return null;
    }

    public static void StartMonitors()
    {
      if (monitoring.Count != 0)
        return;

      foreach (Type type in monitors)
      {
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        Monitor monitor = (Monitor)constructor.Invoke(null);
        try
        {
          monitor.Start();
          monitoring.Add(monitor);
        }
        catch (Exception)
        {
        }
      }
    }

    public static void StopMonitors()
    {
      foreach (Monitor monitor in monitoring)
      {
        try
        {
          monitor.Stop();
        }
        catch (Exception)
        {
        }
      }
      monitoring.Clear();
    }

    private static XmlElement GetComponent(XmlElement parent, string[] path, int pos)
    {
      if (pos == path.Length)
        return parent;

      XmlNode node = Config.FirstChild;
      while (node != null)
      {
        if ((node is XmlElement) && (node.Name == "component") && (((XmlElement)node).GetAttribute("id") == path[pos]))
          return GetComponent((XmlElement)node, path, pos + 1);
      }
      XmlElement element = parent.OwnerDocument.CreateElement("component", AUDIT_NS);
      element.SetAttribute("id", path[pos]);
      parent.AppendChild(element);
      return GetComponent(element, path, pos + 1);
    }

    private static void ExecuteAuditor(Auditor auditor, XmlElement el)
    {
      XmlElement element = GetComponent(el, auditor.Component, 0);
      auditor.Audit(element);
    }

    public static XmlDocument PerformFullAudit()
    {
      XmlDocument document = new XmlDocument();
      document.AppendChild(document.CreateElement("audit", AUDIT_NS));

      document.DocumentElement.SetAttribute("id", Config.GetAttribute("id"));
      DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      TimeSpan time = DateTime.Now.ToUniversalTime() - epoch;
      document.DocumentElement.SetAttribute("date", Math.Round(time.TotalMilliseconds).ToString());

      IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
      document.DocumentElement.SetAttribute("hostname", props.HostName);
      document.DocumentElement.SetAttribute("domainname", props.DomainName);

      foreach (Type type in auditors)
      {
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        Auditor auditor = (Auditor)constructor.Invoke(null);
        ExecuteAuditor(auditor, document.DocumentElement);
      }
      return document;
    }

    private static bool InternalTransmitItem(XmlDocument document, string url)
    {
      try
      {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //WebProxy proxy = new WebProxy("http://localhost:8888/",false);
        //request.Proxy = proxy;
        request.Method = "POST";
        request.AllowWriteStreamBuffering = true;
        request.ContentType = "text/xml";
        
        Stream stream = request.GetRequestStream();
        XmlUtils.SaveXml(document, stream);
        stream.Flush();
        stream.Close();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        if (response.StatusCode == HttpStatusCode.OK)
        {
          XmlDocument received = XmlUtils.LoadXml(response.GetResponseStream());

          string id = document.DocumentElement.GetAttribute("id");
          if (id == Config.GetAttribute("id"))
          {
            config = received;
            FlushConfig();
          }
          else
          {
            DirectoryInfo confdir = DataDir.CreateSubdirectory("Config");
            FileInfo conffile = new FileInfo(confdir.FullName + "\\" + id + ".xml");
            if (conffile.Exists)
            {
              int version = int.Parse(received.DocumentElement.GetAttribute("version"));

              XmlDocument previous = XmlUtils.LoadXml(conffile);
              int prevversion = int.Parse(previous.DocumentElement.GetAttribute("version"));

              if (version<=prevversion)
                return true;
            }

            XmlUtils.SaveXml(received, conffile);
          }

          return true;
        }
      }
      catch (Exception e)
      {
        Debug.Write(e);
      }
      return false;
    }

    private static bool InternalTransmitItem(XmlDocument document)
    {
      return InternalTransmitItem(document, "http://audit.blueprintit.co.uk/submit");
    }

    public static bool TransmitItem(XmlDocument document, bool useCache)
    {
      if (useCache)
      {
        if (InternalTransmitItem(document))
        {
          TransmitCachedItems();
          return true;
        }
        else
        {
          CacheItem(document);
          return false;
        }
      }
      else
        return InternalTransmitItem(document);
    }

    public static void TransmitCachedItems()
    {
      DirectoryInfo cache = DataDir.CreateSubdirectory("Cache");
      foreach (FileInfo file in cache.GetFiles("*.xml"))
      {
        XmlDocument document = XmlUtils.LoadXml(file);

        if (InternalTransmitItem(document))
          file.Delete();
        else
          break;
      }
    }

    public static void CacheItem(XmlDocument document)
    {
      DirectoryInfo cache = DataDir.CreateSubdirectory("Cache");
      Guid guid;
      do
      {
        guid = Guid.NewGuid();
      } while (File.Exists(cache.FullName + "\\" + guid.ToString() + ".xml"));

      XmlUtils.SaveXml(document, cache.FullName + "\\" + guid.ToString() + ".xml");
    }

    #region Auditor implementation
    public string ID
    {
      get { return "auditor"; }
    }

    public string[] Component
    {
      get { return new string[] { ID }; }
    }

    private void AuditAssembly(IList<Assembly> assemblys, Assembly assembly, XmlElement el)
    {
      if (assemblys.Contains(assembly))
        return;

      XmlElement ass = el.OwnerDocument.CreateElement("component", AUDIT_NS);
      el.AppendChild(ass);
      ass.SetAttribute("id", assembly.ManifestModule.Name);
      ass.SetAttribute("name", assembly.FullName);
      assemblys.Add(assembly);
    }

    public void Audit(XmlElement element)
    {
      IList<Assembly> assemblys = new List<Assembly>();
      AuditAssembly(assemblys, Assembly.GetEntryAssembly(), element);
      AuditAssembly(assemblys, Assembly.GetExecutingAssembly(), element);

      foreach (Type type in auditors)
      {
        AuditAssembly(assemblys, type.Assembly, element);
      }

      foreach (Type type in monitors)
      {
        AuditAssembly(assemblys, type.Assembly, element);
      }
    }
    #endregion
  }

  public interface Auditor
  {
    string ID
    {
      get;
    }

    string[] Component
    {
      get;
    }

    void Audit(XmlElement element);
  }

  public interface Monitor
  {
    string ID
    {
      get;
    }

    void Start();
    void Stop();
  }
}
