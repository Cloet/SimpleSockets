using System;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class FileInfoSerializable {

    private readonly FileInfo _fileInfo;

    private readonly DirectoryInfo _directoryInfo;

    [JsonConstructor]
    private FileInfoSerializable() {

    }

    public FileInfoSerializable(FileInfo fileInfo) {
        _fileInfo = fileInfo;

        if (_fileInfo != null) {
            _name = _fileInfo.Name;
            _fullName = _fileInfo.FullName;
            _length = _fileInfo.Length;
            _extension = _fileInfo.Extension;
            _lastWriteTime = _fileInfo.LastWriteTime;
            _dirname = _fileInfo.DirectoryName;
        }
    }

    public FileInfoSerializable(DirectoryInfo directoryInfo) {
        _directoryInfo = directoryInfo;

        if (_directoryInfo != null) {
            _name = _directoryInfo.Name;
            _fullName = _directoryInfo.FullName;
            _length = 0;
            _extension = "";
            _lastWriteTime = _directoryInfo.LastWriteTime;
            _dirname = "";
        }
    }

    private string _name;
    private string _fullName;
    private long _length;
    private string _extension;
    private DateTime _lastWriteTime;
    private string _dirname;


    public string Name { 
        get => _name; 
        set => _name = value;
    }

    public string FullName { 
        get => _fullName; 
        set => _fullName = value;
    }

    public long Length { 
        get => _length; 
        set => _length = value;
    }

    public string Extension { 
        get => _extension; 
        set => _extension = value;
    }

    public DateTime LastWriteTime { 
        get => _lastWriteTime; 
        set => _lastWriteTime = value;
    }

    public string DirectoryName { 
        get => _dirname; 
        set => _dirname = value;
    }


}