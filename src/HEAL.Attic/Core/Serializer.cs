﻿#region License Information
/*
 * This file is part of HEAL.Attic which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System.IO;
using System.IO.Compression;
using System.Threading;

namespace HEAL.Attic {
  public abstract class Serializer : ISerializer {
    public virtual void Serialize(object o, Stream stream,
                                  bool disposeStream = true,
                                  CancellationToken cancellationToken = default(CancellationToken)) {
      Serialize(o, stream, out SerializationInfo _, disposeStream, cancellationToken);
    }
    public virtual void Serialize(object o, Stream stream,
                                  out SerializationInfo info,
                                  bool disposeStream = true,
                                  CancellationToken cancellationToken = default(CancellationToken)) {
      SerializeBundle(Mapper.ToBundle(o, out info, cancellationToken), stream, disposeStream);
    }
    public virtual void Serialize(object o, string path,
                                  CancellationToken cancellationToken = default(CancellationToken)) {
      Serialize(o, path, out SerializationInfo _, cancellationToken);
    }
    public virtual void Serialize(object o, string path,
                                  out SerializationInfo info,
                                  CancellationToken cancellationToken = default(CancellationToken)) {
      string tempfile = Path.GetTempFileName();

      using (FileStream stream = File.Create(tempfile)) {
        using (var zipStream = new DeflateStream(stream, CompressionMode.Compress)) {
          Serialize(o, zipStream, out info, false, cancellationToken);
        }
      }
      if (!cancellationToken.IsCancellationRequested) {
        File.Copy(tempfile, path, true);
      }
      File.Delete(tempfile);
    }
    public virtual byte[] Serialize(object o, CancellationToken cancellationToken = default(CancellationToken)) {
      return Serialize(o, out SerializationInfo _);
    }
    public virtual byte[] Serialize(object o,
                                    out SerializationInfo info,
                                    CancellationToken cancellationToken = default(CancellationToken)) {
      using (var memoryStream = new MemoryStream()) {
        using (var zipStream = new DeflateStream(memoryStream, CompressionMode.Compress)) {
          Serialize(o, zipStream, out info, false, cancellationToken);
        }
        return memoryStream.ToArray();
      }
    }
    protected abstract void SerializeBundle(Bundle bundle, Stream stream,
                                            bool disposeStream = true);

    public virtual object Deserialize(Stream stream, bool disposeStream = true) {
      return Deserialize(stream, out SerializationInfo _, disposeStream);
    }
    public virtual object Deserialize(Stream stream, out SerializationInfo info, bool disposeStream = true) {
      return Mapper.ToObject(DeserializeBundle(stream, disposeStream), out info);
    }
    public virtual object Deserialize(string path) {
      return Deserialize(path, out SerializationInfo _);
    }
    public virtual object Deserialize(string path, out SerializationInfo info) {
      using (var fileStream = new FileStream(path, FileMode.Open)) {
        using (var zipStream = new DeflateStream(fileStream, CompressionMode.Decompress)) {
          return Deserialize(zipStream, out info);
        }
      }
    }
    public virtual object Deserialize(byte[] data) {
      return Deserialize(data, out SerializationInfo _);
    }
    public virtual object Deserialize(byte[] data, out SerializationInfo info) {
      using (var memoryStream = new MemoryStream(data)) {
        using (var zipStream = new DeflateStream(memoryStream, CompressionMode.Decompress)) {
          return Deserialize(zipStream, out info);
        }
      }
    }
    protected abstract Bundle DeserializeBundle(Stream stream, bool disposeStream = true);
  }
}