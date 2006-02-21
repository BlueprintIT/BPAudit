using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace BlueprintIT.HttpServer
{
  public delegate void RequestHandler(HttpRequest request);

  class HttpServer
  {
    private IList<IPEndPoint> addresses = new List<IPEndPoint>();
    private IList<Thread> threads = new List<Thread>();
    private bool started = false;

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
        Thread.Sleep(100);
      }
    }

    private void Handle(object cl)
    {
      TcpClient client = (TcpClient)cl;
      Stream stream = client.GetStream();
      while (client.Connected)
      {
        HttpRequest request = new HttpRequest(client, stream);
        request.Close();
        if (request.RequestHeaders["Connection"] == "close")
        {
          client.Close();
          break;
        }
      }
    }
  }
}
