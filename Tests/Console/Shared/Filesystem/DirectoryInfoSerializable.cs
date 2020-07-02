using System;
using System.IO;
using Newtonsoft.Json;

namespace Shared.FileSystem {

    [Serializable]
    public class DirectoryInfoSerializable {


        public string Name { get; set;}

        public string FullName { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public string ParentFullName { get; set; }

        public string ParentName { get; set; }

        [JsonConstructor]
        private DirectoryInfoSerializable() {

        }

        public DirectoryInfoSerializable(DirectoryInfo directoryInfo) {

            if (directoryInfo != null) {
                Name = directoryInfo.Name;
                FullName = directoryInfo.FullName;
                LastWriteTime = directoryInfo.LastWriteTime;
                CreationTime = directoryInfo.CreationTime;
                ParentFullName = directoryInfo.Parent.FullName;
                ParentName = directoryInfo.Parent.Name;
            }

        }




    }

}