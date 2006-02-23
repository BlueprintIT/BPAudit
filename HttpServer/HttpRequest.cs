using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO.Compression;
using System.Diagnostics;
using System.Xml;

namespace BlueprintIT.HttpServer
{
  public class HttpRequest
  {
    private string method;
    private string path;
    private string version;
    private int statusCode = 200;
    private string statusText = "OK";

    private TcpClient client;
    private Stream stream;
    private bool closed = false;
    private Stream requestStream;
    private Stream responseStream;

    private IDictionary<string, string> requestHeaders = new Dictionary<string, string>();
    private IDictionary<string, string> responseHeaders = new Dictionary<string, string>();

    internal HttpRequest(TcpClient client, Stream stream)
    {
      this.stream = stream;
      this.client = client;

      requestHeaders["Host"] = "localhost";
      requestHeaders["Connection"] = "keep-alive";

      string line = ReadLine(stream);
      int pos = line.LastIndexOf("HTTP/");
      version = line.Substring(pos + 5).TrimEnd();
      line = line.Substring(0, pos);
      pos = line.IndexOf(" ");
      method = line.Substring(0, pos);
      path = line.Substring(pos + 1).Trim();

      Debug.WriteLine("Read request: \"" + method + "\" \"" + path + "\"");

      if (version == "1.0")
      {
        requestHeaders["Connection"] = "close";
      }

      line = ReadLine(stream);
      while (line.Length > 0)
      {
        DecodeHeader(line);
        line = ReadLine(stream);
      }

      if ((requestHeaders.ContainsKey("Transfer-Encoding")) || (requestHeaders.ContainsKey("Content-Length")))
      {
        DecodeBody();
      }

      if (version == "1.0")
      {
        responseStream = new IdentityTransferStream(stream, false, true);
      }
      else
      {
        responseHeaders["Transfer-Encoding"] = "chunked";
        responseStream = new ChunkedTransferStream(stream, false, true);
      }
    }

    private void DecodeHeader(string line)
    {
      int pos = line.IndexOf(":");
      string name = line.Substring(0, pos).Trim();
      string value = line.Substring(pos + 1).Trim();
      requestHeaders[name] = value;
    }

    private void DecodeBody()
    {
      requestStream = stream;
      if ((requestHeaders.ContainsKey("Transfer-Encoding")) && (requestHeaders["Transfer-Encoding"] == "chunked"))
        requestStream = new ChunkedTransferStream(requestStream, true, false);
      else if (requestHeaders.ContainsKey("Content-Length"))
        requestStream = new IdentityTransferStream(requestStream, true, false, int.Parse(requestHeaders["Content-Length"]));
      else
        requestStream = new IdentityTransferStream(requestStream, true, false);

      if (requestHeaders.ContainsKey("Content-Encoding"))
      {
        if (requestHeaders["Content-Encoding"] == "gzip")
        {
          requestStream = new GZipStream(requestStream, CompressionMode.Decompress);
        }
        else if (requestHeaders["Content-Encoding"] == "deflate")
        {
          requestStream = new DeflateStream(requestStream, CompressionMode.Decompress);
        }
      }
    }

    internal static string ReadLine(Stream stream)
    {
      byte[] buffer = new byte[1024];
      int pos = 0;
      do
      {
        int current = stream.ReadByte();
        if (current < 0)
          throw new EndOfStreamException("Unexpected end of stream.");

        buffer[pos] = (byte)current;
        pos++;
      } while ((buffer[pos - 1] != 10) || (buffer[pos - 2] != 13));
      return Encoding.ASCII.GetString(buffer, 0, pos - 2);
    }

    public void SendResponseHeaders()
    {
      if (responseHeaders != null)
      {
        byte[] bytes;
        bytes = Encoding.ASCII.GetBytes("HTTP/"+version+" "+statusCode+" "+StatusText+"\r\n");
        stream.Write(bytes, 0, bytes.Length);
        foreach (string key in responseHeaders.Keys)
        {
          string line = key + ": " + responseHeaders[key] + "\r\n";
          bytes = Encoding.ASCII.GetBytes(line);
          stream.Write(bytes, 0, bytes.Length);
        }
        bytes = Encoding.ASCII.GetBytes("\r\n");
        stream.Write(bytes, 0, bytes.Length);
        responseHeaders = null;
      }
    }

    public void SendResponse(string response)
    {
      responseHeaders["Content-Length"] = Encoding.UTF8.GetByteCount(response).ToString();
      TextWriter writer = new StreamWriter(ResponseStream, Encoding.UTF8);
      writer.Write(response);
      writer.Close();
      Close();
    }

    public void SendResponseXML(XmlDocument document)
    {
      StringBuilder content = new StringBuilder();
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.Indent = true;
      settings.IndentChars = "  ";
      XmlWriter writer = XmlWriter.Create(content, settings);
      document.WriteTo(writer);
      writer.Flush();
      writer.Close();
      SendResponse(content.ToString());
    }

    public void Close()
    {
      if (!closed)
      {
        closed = true;
        if (requestStream!=null)
          requestStream.Close();
        SendResponseHeaders();
        responseStream.Close();
      }
    }

    public int StatusCode
    {
      get { return statusCode; }
      set { statusCode = value; }
    }

    public string StatusText
    {
      get { return statusText; }
      set { statusText = value; }
    }

    public TcpClient Client
    {
      get { return client; }
    }

    public string Method
    {
      get { return method; }
    }

    public string Path
    {
      get { return path; }
    }

    public string Host
    {
      get { return requestHeaders["Host"]; }
    }

    public string HttpVersion
    {
      get { return version; }
    }

    public string RequestContentType
    {
      get { return requestHeaders["Content-Type"]; }
    }

    public string ResponseContentType
    {
      get { return responseHeaders["Content-Type"]; }
      set { responseHeaders["Content-Type"] = value; }
    }

    public IDictionary<string, string> RequestHeaders
    {
      get { return requestHeaders; }
    }

    public IDictionary<string, string> ResponseHeaders
    {
      get { return responseHeaders; }
    }

    public Stream RequestStream
    {
      get { return requestStream; }
    }

    public Stream ResponseStream
    {
      get
      {
        SendResponseHeaders();
        return responseStream;
      }
    }
  }
}
