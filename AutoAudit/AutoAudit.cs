using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using BlueprintIT.Audit;

namespace AutoAudit
{
  public partial class AutoAudit : ServiceBase
  {
    private HttpListener listener;

    public AutoAudit()
    {
      InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
      listener = new HttpListener();
      listener.Prefixes.Add("http://*:16976/");
      listener.Start();
      listener.BeginGetContext(new AsyncCallback(RequestHandler), null);
      AuditManager.StartMonitors();
    }

    protected override void OnStop()
    {
      AuditManager.StopMonitors();
      listener.Stop();
    }

    private void RequestHandler(IAsyncResult target)
    {
      if (!listener.IsListening)
        return;

      listener.BeginGetContext(new AsyncCallback(RequestHandler), null);

      HttpListenerContext context = listener.EndGetContext(target);
      HttpListenerRequest request = context.Request;

      if (request.Url.PathAndQuery != "/submit")
      {
        context.Response.StatusCode = 404;
        context.Response.StatusDescription = "Not Found";
        context.Response.Close();
        return;
      }

      if (request.HttpMethod != "POST")
      {
        context.Response.StatusCode = 405;
        context.Response.StatusDescription = "Method Not Allowed";
        context.Response.Close();
        return;
      }

      if (request.ContentType != "text/xml")
      {
        context.Response.StatusCode = 415;
        context.Response.StatusDescription = "Unsupported Media Type";
        context.Response.Close();
        return;
      }

      XmlDocument document;
      try
      {
        XmlReaderSettings settings = new XmlReaderSettings();
        TextReader input = new StreamReader(request.InputStream, Encoding.UTF8);
        XmlReader reader = XmlReader.Create(input, settings);
        document = new XmlDocument();
        document.Load(reader);
        reader.Close();
      }
      catch (Exception)
      {
        context.Response.StatusCode = 500;
        context.Response.StatusDescription = "Internal Server Error";
        context.Response.Close();
        return;
      }
      Debug.WriteLine("Accepted item from remote host");

      AuditManager.TransmitItem(document);
      context.Response.StatusCode = 200;
      context.Response.StatusDescription = "OK";
      context.Response.Close();
    }
  }
}
