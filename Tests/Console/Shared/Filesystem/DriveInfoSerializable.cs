using System;
using System.IO;
using Newtonsoft.Json;

namespace Shared.FileSystem {

    [Serializable]
    public class DriveInfoSerializable {

        public string Name { get; set; }

        public long AvailableFreeSpace { get; set; }

        public string DriveFormat { get; set; }

        public long TotalSize { get; set; }

        [JsonConstructor]
        private DriveInfoSerializable() {

        }

        public DriveInfoSerializable(DriveInfo driveInfo) {

            if (driveInfo != null) {
                Name = driveInfo.Name;
                AvailableFreeSpace = driveInfo.AvailableFreeSpace;
                DriveFormat = driveInfo.DriveFormat;
                TotalSize =  driveInfo.TotalSize;
            }

        }


    }

}