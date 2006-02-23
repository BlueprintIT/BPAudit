using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace BlueprintIT.HttpServer
{
  public delegate void RequestHandler(HttpRequest request);

  public class HttpServer
  {
    private IList<IPEndPoint> addresses = new List<IPEndPoint>();
    private IList<Thread> threads = new List<Thread>();
    private bool started = false;

    private RequestHandler defaultHandler;

    private WaitCallback callback;

    public HttpServer()
    {
      callback = new WaitCallback(Handle);
    }

    public HttpServer(int port) : this()
    {
      addresses.Add(new IPEndPoint(IPAddress.Any, port));
    }

    public HttpServer(IPAddress address, int port) : this()
    {
      addresses.Add(new IPEndPoint(address, port));
    }

    public RequestHandler DefaultHandler
    {
      get
      {
        return defaultHandler;
      }

      set
      {
        defaultHandler = value;
      }
    }

    public void Start()
    {
      started = true;
      foreach (IPEndPoint address in addresses)
      {
        Thread thread = new Thread(new ParameterizedThreadStart(Serve));
        thread.Start(address);
        threads.Add(thread);
      }
    }

    public void Stop()
    {
      started = false;
      foreach (Thread thread in threads)
      {
        thread.Interrupt();
      }
    }

    private void Serve(object address)
    {
      IPEndPoint endpoint = (IPEndPoint)address;
      TcpListener listener = new TcpListener(endpoint);
      listener.Start();
      while (started)
      {
        while (listener.Pending())
        {
          TcpClient client = listener.AcceptTcpClient();
          if (client.Connected)
            ThreadPool.QueueUserWorkItem(callback, client);
        }
        try
        {
          Thread.Sleep(100);
        }
        catch (ThreadInterruptedException)
        {
        }
      }
    }

    private void Handle(object cl)
    {
      Debug.WriteLine("Handling new client");
      TcpClient client = (TcpClient)cl;
      Stream stream = client.GetStream();
      try
      {
        while (client.Connected)
        {
          HttpRequest request = new HttpRequest(client, stream);
          try
          {
            defaultHandler(request);
          }
          catch (Exception e)
          {
          }
          request.Close();
          if (request.RequestHeaders["Connection"] == "close")
          {
            client.Close();
            break;
          }
        }
      }
      catch (Exception e)
      {
      }
    }
  }
}
