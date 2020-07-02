using System.Collections.Generic;

namespace Shared.FileSystem {

    public class FolderContent {

        public IList<FileInfoSerializable> Files { get; set; } = new List<FileInfoSerializable>();

        public IList<DirectoryInfoSerializable> Directories { get; set; } = new List<DirectoryInfoSerializable>();

    }

}