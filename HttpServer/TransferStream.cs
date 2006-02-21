using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace BlueprintIT.HttpServer
{
  public class ChunkedTransferStream : Stream
  {
    private Stream stream;
    private bool canRead;
    private bool canWrite;

    private byte[] readBuffer;
    private int readPos;
    private bool readComplete = false;
    
    private byte[] writeBuffer;
    private int writePos;
    private bool writeComplete = false;

    public ChunkedTransferStream(Stream stream, bool canRead, bool canWrite)
    {
      this.stream = stream;
      this.canRead = canRead;
      this.canWrite = canWrite;

      if (canWrite)
        writeBuffer = new byte[10240];
    }

    public override bool CanRead
    {
      get { return canRead; }
    }

    public override bool CanSeek
    {
      get { return false; }
    }

    public override bool CanWrite
    {
      get { return canWrite; }
    }

    public override void Close()
    {
      if (canWrite)
      {
        Flush();
        byte[] bytes = Encoding.ASCII.GetBytes("0\r\n\r\n");
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        writeComplete = true;
      }

      if ((canRead) && (!readComplete))
      {
        byte[] buffer = new byte[10240];
        while (Read(buffer, 0, buffer.Length) > 0) ;
      }
    }

    public override void Flush()
    {
      if ((canWrite) && (writePos > 0))
      {
        byte[] bytes = Encoding.ASCII.GetBytes(writePos + "\r\n");
        stream.Write(bytes, 0, bytes.Length);
        stream.Write(writeBuffer, 0, writePos);
        bytes = Encoding.ASCII.GetBytes("\r\n");
        stream.Write(bytes, 0, bytes.Length);
        writePos = 0;
        stream.Flush();
      }
    }

    public override long Length
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public override long Position
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public override void SetLength(long value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    private bool ReadNewChunk()
    {
      int readLength = int.Parse(HttpRequest.ReadLine(stream), NumberStyles.HexNumber);
      if (readLength > 0)
      {
        readBuffer = new byte[readLength];
        readPos = 0;
        int pos = 0;
        while (pos < readLength)
        {
          int read = stream.Read(readBuffer, pos, readLength - pos);
          if (read == 0)
            throw new EndOfStreamException("Unexpected end of stream");

          pos += read;
        }
        HttpRequest.ReadLine(stream);
        return true;
      }
      string line = HttpRequest.ReadLine(stream);
      while (line.Length > 0)
      {
        line = HttpRequest.ReadLine(stream);
      }
      return false;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (!canRead)
        throw new Exception("The method or operation is not implemented.");

      if (readComplete)
        throw new EndOfStreamException("Attempt to read past end of stream.");

      if ((readBuffer == null) || (readPos == readBuffer.Length))
      {
        if (!ReadNewChunk())
          return 0;
      }

      int length = Math.Min(count, readBuffer.Length - readPos);
      Array.Copy(readBuffer, readPos, buffer, offset, length);
      readPos += length;
      return length;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      if (!canWrite)
        throw new Exception("The method or operation is not implemented.");

      if (writeComplete)
        throw new EndOfStreamException("Attempt to write past end of stream.");

      if ((writePos + count) > writeBuffer.Length)
      {
        Array.Copy(buffer, offset, writeBuffer, writePos, writeBuffer.Length - writePos);
        offset += writeBuffer.Length - writePos;
        count -= writeBuffer.Length - writePos;
        Flush();
        Write(buffer, offset, count);
      }
      else
      {
        Array.Copy(buffer, offset, writeBuffer, writePos, count);
        writePos += count;
      }
    }
  }

  public class IdentityTransferStream : Stream
  {
    private Stream stream;
    private bool canRead;
    private bool canWrite;

    private int readLength;
    private int readPos = 0;
    private bool readComplete = false;

    public IdentityTransferStream(Stream stream, bool canRead, bool canWrite, int readLength)
    {
      this.stream = stream;
      this.canRead = canRead;
      this.canWrite = canWrite;
      this.readLength = readLength;
    }

    public IdentityTransferStream(Stream stream, bool canRead, bool canWrite) : this(stream, canRead, canWrite, -1)
    {
    }

    public override bool CanRead
    {
      get { return canRead; }
    }

    public override bool CanSeek
    {
      get { return false; }
    }

    public override bool CanWrite
    {
      get { return canWrite; }
    }

    public override void Close()
    {
      if (canWrite)
        stream.Flush();

      if ((canRead) && (!readComplete))
      {
        byte[] buffer = new byte[10240];
        while (Read(buffer, 0, buffer.Length) > 0) ;
      }
    }

    public override void Flush()
    {
      if (canWrite)
        stream.Flush();
    }

    public override long Length
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public override long Position
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public override void SetLength(long value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (!canRead)
        throw new Exception("The method or operation is not implemented.");

      if (readComplete)
        throw new EndOfStreamException("Attempt to read past end of stream.");

      if (readPos == readLength)
      {
        readComplete = true;
        return 0;
      }

      if (readLength >= 0)
        count = Math.Min(count, readLength - readPos);

      int read = stream.Read(buffer, offset, count);
      readPos += read;
      if (read == 0)
        readComplete = true;

      return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      if (!canWrite)
        throw new Exception("The method or operation is not implemented.");

      stream.Write(buffer, offset, count);
    }
  }
}
