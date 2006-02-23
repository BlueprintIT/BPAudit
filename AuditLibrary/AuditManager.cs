using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Net;

namespace BlueprintIT.Audit
{
  public class AuditManager
  {
    private static IList<Type> auditors = new List<Type>();
    private static IList<Type> monitors = new List<Type>();
    private static IList<Monitor> monitoring = new List<Monitor>();

    static AuditManager()
    {
      ScanAssembly(Assembly.GetExecutingAssembly());
    }

    private static void ScanAssembly(Assembly assembly)
    {
      Type auditor = Type.GetType("BlueprintIT.Audit.Auditor");
      Type monitor = Type.GetType("BlueprintIT.Audit.Monitor");
      if ((auditor != null) && (monitor != null))
      {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
          if (type.IsSubclassOf(auditor))
            auditors.Add(type);
          if (type.IsSubclassOf(monitor))
            monitors.Add(type);
        }
      }
    }

    public static RegistryKey DataKey
    {
      get { return Registry.LocalMachine.CreateSubKey("Software").CreateSubKey("Blueprint IT").CreateSubKey("Auditor"); }
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

    public static XmlDocument Audit()
    {
      XmlDocument document = new XmlDocument();
      document.AppendChild(document.CreateElement("Audit"));

      string id = (string)DataKey.GetValue("computer", Guid.NewGuid().ToString());
      DataKey.SetValue("computer", id);
      document.DocumentElement.SetAttribute("id", id);
      document.DocumentElement.SetAttribute("date", DateTime.Now.ToUniversalTime().ToShortDateString());

      foreach (Type type in auditors)
      {
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        Auditor auditor = (Auditor)constructor.Invoke(null);
        auditor.Audit(document.DocumentElement);
      }
      return document;
    }

    public static void AuditAndSubmit()
    {
      XmlDocument audit = Audit();
      TransmitItem(audit);
    }

    private static bool InternalTransmitItem(XmlDocument document, string url)
    {
      try
      {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "text/xml";

        StringBuilder content = new StringBuilder();
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = "  ";
        settings.Encoding = Encoding.UTF8;
        settings.CloseOutput = true;

        XmlWriter writer = XmlWriter.Create(request.GetRequestStream(), settings);
        document.WriteTo(writer);
        writer.Flush();
        writer.Close();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        if (response.StatusCode == HttpStatusCode.OK)
          return true;
      }
      catch (Exception e)
      {
        Debug.Write(e);
      }
      return false;
    }

    private static bool InternalTransmitItem(XmlDocument document)
    {
      return InternalTransmitItem(document, "http://localhost:16976/submit");
    }

    public static void TransmitItem(XmlDocument document)
    {
      if (InternalTransmitItem(document))
        TransmitCachedItems();
      else
        CacheItem(document);
    }

    public static void TransmitCachedItems()
    {
      DirectoryInfo cache = DataDir.CreateSubdirectory("Cache");
      foreach (FileInfo file in cache.GetFiles("*.xml"))
      {
        XmlReaderSettings settings = new XmlReaderSettings();
        TextReader input = new StreamReader(file.FullName, Encoding.UTF8);
        XmlReader reader = XmlReader.Create(input, settings);
        XmlDocument document = new XmlDocument();
        document.Load(reader);
        reader.Close();

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
      Stream stream = new FileStream(cache.FullName + "\\" + guid.ToString() + ".xml", FileMode.Create);
      TextWriter output = new StreamWriter(stream, Encoding.UTF8);
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.CloseOutput = true;
      settings.Indent = true;
      settings.IndentChars = "  ";
      settings.Encoding = Encoding.UTF8;
      XmlWriter writer = XmlWriter.Create(output, settings);
      document.WriteTo(writer);
      writer.Flush();
      writer.Close();
    }
  }

  public abstract class Auditor
  {
    public abstract string ID
    {
      get;
    }

    protected RegistryKey DataKey
    {
      get { return AuditManager.DataKey.CreateSubKey(ID); }
    }

    protected DirectoryInfo DataDir
    {
      get { return AuditManager.DataDir.CreateSubdirectory(ID); }
    }

    public abstract void Audit(XmlElement element);
  }

  public abstract class Monitor
  {
    public abstract string ID
    {
      get;
    }

    protected RegistryKey DataKey
    {
      get { return AuditManager.DataKey.CreateSubKey(ID); }
    }

    protected DirectoryInfo DataDir
    {
      get { return AuditManager.DataDir.CreateSubdirectory(ID); }
    }

    public abstract void Start();
    public abstract void Stop();
  }
}
