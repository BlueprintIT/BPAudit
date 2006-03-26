using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using BlueprintIT.Utils;
using System.Diagnostics;

namespace BlueprintIT.Audit
{
  internal class ClientMonitor : Observer
  {
    private HttpListener listener;

    public ClientMonitor()
    {
    }

    public string ID
    {
      get { return "server"; }
    }

    public void Start()
    {
      if (false)
      {
        listener = new HttpListener();
        listener.Prefixes.Add("http://*:16976/");
        listener.Start();
        listener.BeginGetContext(new AsyncCallback(RequestHandler), null);
      }
    }

    public void Stop()
    {
      if (listener != null)
      {
        listener.Stop();
        listener = null;
      }
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
        document = XmlUtils.LoadXml(request.InputStream);
        request.InputStream.Close();
      }
      catch (Exception)
      {
        context.Response.StatusCode = 500;
        context.Response.StatusDescription = "Internal Server Error";
        context.Response.Close();
        return;
      }
      Debug.WriteLine("Accepted item from remote host");

      AuditManager.CacheItem(document);
      HttpListenerResponse response = context.Response;
      response.StatusCode = 200;
      response.StatusDescription = "OK";

      XmlDocument config = AuditManager.GetConfig(document.DocumentElement.GetAttribute("id"));
      if (config == null)
      {
        config = new XmlDocument();
        XmlElement conf = config.CreateElement("config", AuditManager.AUDIT_NS);
        config.AppendChild(conf);
        conf.SetAttribute("version", "0");
        conf.SetAttribute("id", document.DocumentElement.GetAttribute("id"));
      }
      response.ContentType = "text/xml";

      XmlUtils.SaveXml(config, response.OutputStream);
      response.OutputStream.Flush();
      response.OutputStream.Close();
      response.Close();
    }
  }
}
