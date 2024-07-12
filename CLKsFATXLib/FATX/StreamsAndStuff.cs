﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CLKsFATXLib.Extensions;
using System.Management;

namespace CLKsFATXLib
{
    /// <summary>
    /// Provides the GetFATXDrives function for getting a list of all FATX-formatted drives connected to the computer.
    /// </summary>
    static public class StartHere
    {
        /// <summary>
        /// Gets the FATX-formatted drives connected to the computer.
        /// </summary>
        /// <returns>List of all FATX drives connected to the computer</returns>
        static public Drive[] GetFATXDrives()
        {
            // Our drive list
            List<Drive> dL = new List<Drive>();
            int Count = 10;
            try
            {
                ManagementObjectCollection drives = new ManagementObjectSearcher(
         "SELECT Caption, DeviceID FROM Win32_DiskDrive").Get();
                Count = drives.Count;
            }
            catch { Console.WriteLine("Encountered error when trying to open managed object collection: GetFATXDrives();"); }
            for (int i = 0; i < Count; i++)
            {
                Drive d = new Drive(i);
                if (d.IsFATXDrive())
                {
                    dL.Add(d);
                }
            }
            // Sort out our USB/logical drives...
            foreach (string s in Environment.GetLogicalDrives().Where(drive => System.IO.Directory.Exists(drive + "\\Xbox360")))
            {
                List<string> filePaths = new List<string>();
                for (int i = 0; i < 10000; i++)
                {
                    string extra = "";
                    if (i < 10)
                    {
                        extra = "000";
                    }
                    else if (i < 100)
                    {
                        extra = "00";
                    }
                    else if (i < 1000)
                    {
                        extra = "0";
                    }
                    if (System.IO.File.Exists(s + "\\Xbox360\\Data" + extra + i.ToString()))
                    {
                        filePaths.Add(s + "\\Xbox360\\Data" + extra + i.ToString());
                    }
                    else { break; }
                }
                if (filePaths.Count >= 3 && !IsLocked(filePaths[0]))
                {
                    Drive d = new Drive(filePaths.ToArray());
                    dL.Add(d);
                }
            }

            return dL.ToArray();
        }

        private static bool IsLocked(string FilePath)
        {
            try
            {
                using (FileStream fs = new System.IO.FileStream(FilePath, FileMode.Open))
                {
                    fs.Close();
                }
                // The file is not locked
                return false;
            }
            catch (Exception)
            {
                return true;
                // The file is locked
            }
        }
    }
}

namespace CLKsFATXLib.Streams
{

    /* I actually didn't recode these because I figured they work fine... */

    public class Writer : System.IO.BinaryWriter
    {
        public Writer(string[] ye)
            : base(new USBStream(ye, System.IO.FileMode.Open))
        {

        }

        public Writer(Stream stream)
            : base(stream)
        {

        }
    }

    public class Reader : System.IO.BinaryReader
    {
        public Reader(string[] ye)
            : base(new USBStream(ye, System.IO.FileMode.Open))
        {

        }

        public Reader(Stream stream)
            : base(stream)
        {

        }

        public Reader(string Path, System.IO.FileMode FileMode)
            : base(new System.IO.FileStream(Path, FileMode))
        {

        }

        public override void Close()
        {
            base.Close();
        }

        public ushort ReadUInt16(bool LittleEndian)
        {
            if (!LittleEndian)
            {
                byte[] buffer = ReadBytes(0x2);
                Array.Reverse(buffer);
                return BitConverter.ToUInt16(buffer, 0x0);
            }
            return base.ReadUInt16();
        }

        public int ReadInt24(bool LittleEndian)
        {
            if (!LittleEndian)
            {
                byte[] Buffer = ReadBytes(0x3);
                Buffer = new byte[]
                {
                    Buffer[2], Buffer[1], Buffer[0], 0,
                };
                return (BitConverter.ToInt32(Buffer, 0x0) >> 8);
            }
            else
            {
                byte[] Buffer = ReadBytes(0x3);
                Buffer = new byte[]
            {
                Buffer[2], Buffer[1], Buffer[0], 0,
            };
                Array.Reverse(Buffer);
                return (BitConverter.ToInt32(Buffer, 0x0) << 8);
            }
        }

        public int ReadInt24()
        {
            byte[] Buffer = ReadBytes(0x3);
            // Reverse the array, add a zero
            Buffer = new byte[]
            {
                Buffer[2], Buffer[1], Buffer[0], 0,
            };
            return (BitConverter.ToInt32(Buffer, 0x0) >> 8);
        }

        public int ReadInt32(bool LittleEndian)
        {
            if (!LittleEndian)
            {
                byte[] buffer = ReadBytes(0x4);
                Array.Reverse(buffer);
                return BitConverter.ToInt32(buffer, 0x0);
            }
            return base.ReadInt32();
        }

        public uint ReadUInt32(bool LittleEndian)
        {
            if (!LittleEndian)
            {
                byte[] Buffer = ReadBytes(0x4);
                Array.Reverse(Buffer);
                return BitConverter.ToUInt32(Buffer, 0x0);
            }
            return base.ReadUInt32();
        }

        public override uint ReadUInt32()
        {
            byte[] Buffer = ReadBytes(0x4);
            Array.Reverse(Buffer);
            return BitConverter.ToUInt32(Buffer, 0x0);
        }

        public override ushort ReadUInt16()
        {
            byte[] buffer = ReadBytes(0x2);
            Array.Reverse(buffer);
            return BitConverter.ToUInt16(buffer, 0x0);
        }

        public override int ReadInt32()
        {
            byte[] Buffer = ReadBytes(0x4);
            Array.Reverse(Buffer);
            return BitConverter.ToInt32(Buffer, 0x0);
        }

        public string ReadUnicodeString(int length)
        {
            string ss = "";
            for (int i = 0; i < length; i += 2)
            {
                char c = (char)ReadUInt16();
                if (c != '\0')
                {
                    ss += c;
                }
            }
            return ss;
        }

        public string ReadCString()
        {
            string ss = "";
            for (int i = 0; ; i += 2)
            {
                char c = (char)ReadUInt16();
                if (c != '\0')
                {
                    ss += c;
                }
                else
                {
                    break;
                }
            }
            return ss;
        }

        public string ReadASCIIString(int length)
        {
            return Encoding.ASCII.GetString(ReadBytes(length));
        }
    }

    public class USBStream : System.IO.Stream
    {
        int Current = 0;
        System.IO.Stream[] Streams;
        public USBStream(string[] filePaths, System.IO.FileMode mode)
            : base()
        {
            Streams = new FileStream[filePaths.Length];
            for (int i = 0; i < Streams.Length; i++)
            {
                Streams[i] = new FileStream(filePaths[i], mode);
            }
        }

        public USBStream(System.IO.Stream[] Streams)
            : base()
        {
            this.Streams = Streams;
        }

        public override bool CanRead
        {
            get { return Streams[Current].CanRead; }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { return Streams[Current].CanRead; }
        }

        public override void Flush()
        {
            Streams[Current].Flush();
        }

        public override long Length
        {
            get
            {
                long length = 0;
                for (int i = 0; i < Streams.Length; i++)
                {
                    length += Streams[i].Length;
                }
                return length;
            }
        }

        public override long Position
        {
            get
            {
                // Loop through each stream before this one, and add that
                // to the return position
                long r3 = 0;
                for (int i = 0; i < Current; i++)
                {
                    // Add the length
                    r3 += Streams[i].Length;
                }
                // Add the position in our current stream
                return r3 + Streams[Current].Position;
            }
            set
            {
                // Reset the position in each stream
                for (int i = 0; i < Streams.Length; i++)
                {
                    Streams[i].Position = 0;
                }

                // Determine which stream we need to be on...
                long Remaining = value;
                for (int i = 0; i < Streams.Length; i++)
                {
                    if (Streams[i].Length < Remaining)
                    {
                        Remaining -= Streams[i].Length;
                    }
                    else
                    {
                        Current = i;
                        break;
                    }
                }

                // Check to see if we're at the end of a file...
                if (Remaining == Streams[Current].Length)
                {
                    // We were, so let's bump the current stream up one
                    Current++;
                    return;
                }
                Streams[Current].Position = Remaining;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int DataRead = 0;
            // If the amount of data they're wanting to be read can be
            // read within the current stream...

            // Oh yeah, this is the position before hand... we add the
            // count to this, just to make sure that all of our position are aligned
            long bPos = Position;

            if (Streams[Current].Length - Streams[Current].Position >= count)
            {
                // We can read the data, yaaayyyy
                DataRead += Streams[Current].Read(buffer, offset, count);
            }
            // We can't read it... OH NO! Gotta do some trickery
            else
            {
                // Let's declare out ints here.  First, the data we have to read,
                // then the streams that we have to read from (count), then
                // the amount of data that we can read from this current stream
                long DataLeft = count, streams = 0, DataCurrent = Streams[Current].Length - Streams[Current].Position;

                // Loop through each higher stream, getting the amount of data we can read
                // from each, and if the amount of data is still higher than the data left,
                // then loop again
                for (long i = Current + 1, Remaining = DataLeft - DataCurrent; i < Streams.Length; i++)
                {
                    // Bump up our streams
                    streams++;

                    // If the stream length is smaller than the remaining data...
                    if (Streams[i].Length >= Remaining)
                    {
                        // We can break!
                        break;
                    }
                }

                // Read our beginning data
                DataLeft -= DataRead = Streams[Current].Read(buffer, offset, count);

                // Loop through each stream, reading the rest of the data
                for (int i = 0, cS = (Current + 1); i < streams; i++, cS++)
                {
                    byte[] Temp = new byte[0];
                    if (i == streams - 1)
                    {
                        Temp = new byte[DataLeft];
                    }
                    else
                    {
                        Temp = new byte[Streams[cS].Length];
                    }

                    // Read the data in to our temp array
                    DataRead = Streams[cS].Read(Temp, 0, Temp.Length);

                    // Copy that in to the pointed array
                    Array.Copy(Temp, 0, buffer, count - DataLeft, Temp.Length);

                    DataLeft -= Streams[cS].Length;
                }
            }

            Position = bPos + count;

            // Return count.  Hax.
            return DataRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            /* COPYPASTA FROM READ FUNCTION! */


            // If the amount of data they're wanting to be read can be
            // read within the current stream...

            // Oh yeah, this is the position before hand... we add the
            // count to this, just to make sure that all of our position are aligned
            long bPos = Position;

            if (Streams[Current].Length - Streams[Current].Position >= count)
            {
                // We can write the data, yaaayyyy
                Streams[Current].Write(buffer, offset, count);
            }
            // We can't read it... OH NO! Gotta do some trickery
            else
            {
                // Let's declare out ints here.  First, the data we have to read,
                // then the streams that we have to read from (count), then
                // the amount of data that we can read from this current stream
                long DataLeft = count, streams = 0, DataCurrent = Streams[Current].Length - Streams[Current].Position;

                // Loop through each higher stream, getting the amount of data we can read
                // from each, and if the amount of data is still higher than the data left,
                // then loop again
                for (long i = Current + 1, Remaining = DataLeft - DataCurrent; i < Streams.Length; i++)
                {
                    // Bump up our streams
                    streams++;

                    // If the stream length is smaller than the remaining data...
                    if (Streams[i].Length >= Remaining)
                    {
                        // We can break!
                        break;
                    }
                }

                // Copy the first wave of data in to a temp array
                byte[] Temp = new byte[DataCurrent];
                Array.Copy(buffer, 0, Temp, 0, Temp.Length);

                // Write our beginning data
                Streams[Current].Write(buffer, 0, Temp.Length);
                DataLeft -= Temp.Length;

                // Loop through each stream, reading the rest of the data
                for (int i = 0, cS = (Current + 1); i < streams; i++, cS++)
                {
                    Temp = new byte[0];
                    if (i == streams - 1)
                    {
                        Temp = new byte[DataLeft];
                    }
                    else
                    {
                        Temp = new byte[Streams[cS].Length];
                    }

                    Array.Copy(buffer, count - DataLeft, Temp, 0, Temp.Length);

                    // Read the data in to our temp array
                    Streams[cS].Write(Temp, 0, Temp.Length);

                    DataLeft -= Streams[cS].Length;
                }
            }

            Position = bPos + count;
        }

        public override void Close()
        {
            for (int i = 0; i < Streams.Length; i++)
            {
                Streams[i].Close();
            }
        }
    }

    public class FATXFileStream : Stream
    {
        File xFile;
        long xPositionInFile = 0;
        Stream Underlying;
        byte[] PreviouslyRead = new byte[0];
        long PreviouslyReadOffset = -1;

        public FATXFileStream(string[] InPaths, File file)
        {
            xFile = file;
            // Set our position to the beginning of the file
            long off = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[0], xFile);
            Underlying = new USBStream(InPaths, FileMode.Open);
            //Underlying.Position = off;
            Position = 0;
        }

        public FATXFileStream(int DeviceIndex, System.IO.FileAccess fa, File file)
        {
            xFile = file;
            // Set our position to the beginning of the file
            long off = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[0], xFile);
            Underlying = new FileStream(VariousFunctions.CreateHandle(DeviceIndex), fa);
            //Underlying.Position = off;
            Position = 0;
        }

        public FATXFileStream(string Path, System.IO.FileMode fmode, File file)
        {
            xFile = file;
            // Set our position
            long off = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[0], xFile);
            Underlying = new FileStream(Path, fmode);
            //Underlying.Position = off;
            Position = 0;
        }

        public FATXFileStream(System.IO.Stream Stream, File file)
        {
            xFile = file;
            // Set our position
            long off = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[0], xFile);
            Underlying = Stream;
            //Underlying.Position = off;
            Position = 0;
        }

        public override long Position
        {
            get
            {
                /* If we return the Underlying position, then we're returning the offset
                 * for the entire thing, not just the individual file we're trying
                 * to read*/
                return xPositionInFile;
            }
            set
            {
                if (value > xFile.Size)
                {
                    return;
                    throw new Exception("Can not seek beyond end of file.");
                }
                xPositionInFile = value;
                Underlying.Position = GetRealSectorOffset(value);
            }
        }

        public override void WriteByte(byte value)
        {
            Underlying.WriteByte(value);
        }
         
        public override void Write(byte[] array, int offset, int count)
        {
            if (Position == Length)
            {
                return;
            }
            /* Probably a bad idea, but this is a copy pasta from the read function, modified */

            // This will represent the amount of data we read, and will be our return value.
            int DataRead = 0;

            // This will act as our "resetting" at the end
            long InitialPosition = Position;

            // This int will represent the amount of data we have to remove off of the
            // beginning of the initial read array, due to not being in a 0-based offset
            int beginningDataToRemove = (int)(Position - VariousFunctions.DownToNearest200(Position));

            // Pseudocount is basically there to check and see if ClustersSpanned will be greater than what it should be
            int Pseudocount = (int)(count + (Position - VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize)));
            // Used for keeping the original pseudocount in the loop
            int RealPseudocount = (int)(count + VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize));
            bool EndingCluster = false;
            if (Pseudocount > Length)
            {
                Pseudocount = (int)(VariousFunctions.DownToNearestCluster(Length - Position, xFile.PartitionInfo.ClusterSize) + VariousFunctions.UpToNearestCluster((Length - Position), xFile.PartitionInfo.ClusterSize));
                RealPseudocount = (int)(Length - Position);
                EndingCluster = true;
            }

            // This int will represent the number of clusters that our data spans
            int ClustersSpanned = (int)ClusterSpanned(Pseudocount);

#if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            for (int i = 0; i < ClustersSpanned && (count - DataRead) > 0; i++)
            {
                // This int will represent the amount of data that we can read in this cluster
                int DataReadableCluster = (int)(xFile.PartitionInfo.ClusterSize - (Position - VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize))).UpToNearest200();
                // This int will represent the amount of data we need to add to the array
                int AddToArray = DataReadableCluster;
                // If that number right above is going to be too much data than we need, let's shrink it down
                if (DataReadableCluster > RealPseudocount - DataRead)
                {
                    DataReadableCluster = (int)VariousFunctions.UpToNearest200(beginningDataToRemove + (RealPseudocount - DataRead));
                    // Looks like we're on the last readthrough!
                    AddToArray = RealPseudocount - DataRead;
                }
                else if (DataReadableCluster > count - DataRead)
                {
                    DataReadableCluster = (int)VariousFunctions.UpToNearest200(beginningDataToRemove + (count - DataRead));
                    // Looks like we're on the last readthrough!
                    AddToArray = count - DataRead;
                }
                else if (i == 0 && Position + DataReadableCluster + AddToArray - beginningDataToRemove > xFile.PartitionInfo.ClusterSize)
                {
                    // Leave datareadablecluster alone, change the addtoarray value
                    AddToArray -= beginningDataToRemove;
                }
                if (AddToArray == 0)
                {
                    break;
                }
                // This array will represent a temp array for holding the data to copy
                // to the array PASSED in the arguments
                byte[] TempData = new byte[DataReadableCluster];
                // Set our IO position
                Underlying.Position = GetRealSectorOffset(xPositionInFile);
                // If we've already read this data...
                if (LastRead200Offset == Underlying.Position && DataReadableCluster <= 0x200)
                {
                    // Save us a disk I/O operation!
                    TempData = LastRead200;
                }
                else
                {
                    // Set the LastRead200Offset
                    LastRead200Offset = (AddToArray <= 0x200) ? Underlying.Position : LastRead200Offset;
                    // Read the data
                    Underlying.Read(TempData, 0, DataReadableCluster);
                    // Set the LastRead200 data
                    if (AddToArray <= 0x200)
                    {
                        Array.Copy(TempData, LastRead200, 0x200);
                    }
                }
#if !DEBUG && !TRACE
                if (xFile.Parent.Name == "FFFE07D1")
                {
                    byte[] TempDebug = new byte[AddToArray];
                    Array.Copy(TempData, beginningDataToRemove, TempDebug, 0, AddToArray);
                    uint CRC = Crc32.Compute(TempDebug);
                    byte[] RealFileBuffer = new byte[AddToArray];
                    FileStream fs = System.IO.File.Open(@"C:\Users\Lander\Desktop\E886364B9B6A4F3A", FileMode.Open);
                    fs.Position = Position;
                    fs.Read(RealFileBuffer, 0, AddToArray);
                    fs.Close();
                    uint RealCRC = Crc32.Compute(RealFileBuffer);
                }
#endif
                // Copy the data we read (or got somehow!) over to the output array
                try
                {
                    Array.Copy(array, offset + DataRead, TempData, beginningDataToRemove, AddToArray);
                    Underlying.Position -= DataReadableCluster;
                    Underlying.Write(TempData, 0, TempData.Length);
                    if (LastRead200Offset == Underlying.Position - DataReadableCluster)
                    {
                        Array.Copy(TempData, 0, LastRead200, 0, 0x200);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
#if !DEBUG && !TRACE
                if (xFile.Parent.Name == "FFFE07D1")
                {
                    byte[] TempDebug = new byte[DataRead];
                    Array.Copy(array, TempDebug, DataRead);
                    uint CRC = Crc32.Compute(TempDebug);
                    FileStream fs = System.IO.File.Open(@"C:\Users\Lander\Desktop\E886364B9B6A4F3A", FileMode.Open);
                    fs.Position = InitialPosition;
                    byte[] RealFileBuffer = new byte[DataRead];
                    fs.Read(RealFileBuffer, 0, DataRead);
                    fs.Close();
                    uint RealCRC = Crc32.Compute(RealFileBuffer);
                }
#endif
                // Increase the DataRead value
                DataRead += AddToArray;
                // Increase the position in the file
                Position = InitialPosition + DataRead;
            }

            Position = InitialPosition + DataRead;
        }

        public override long Length
        {
            get
            {
                return xFile.Size;
            }
        }

        // I dont' know why I'VariousFunctions even going to bother with this function.
        // I don't think I'VariousFunctions going to ever use it.
        public override int ReadByte()
        {
            byte[] bA = new byte[1];
            Read(bA, 0, 1);
            return bA[0];
        }

        private byte[] LastRead200 = new byte[0x200];
        private long LastRead200Offset;
        public override int Read(byte[] array, int offset, int count)
        {
            // If we're at the end of the stream, just return 0 since we can't read beyond it!
            if (this.Position == this.Length)
            {
                return 0;
            }

            // This will represent the amount of data we read, and will be our return value.
            int DataRead = 0;

            // This will act as our "resetting" at the end
            long InitialPosition = Position;

            // This int will represent the amount of data we have to remove off of the
            // beginning of the initial read array, due to not being in a 0-based offset
            int beginningDataToRemove = (int)(Position - VariousFunctions.DownToNearest200(Position));

            // Pseudocount is basically there to check and see if ClustersSpanned will be greater than what it should be
            int Pseudocount = (int)(count + (Position - VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize)));
            // Used for keeping the original pseudocount in the loop
            int RealPseudocount = (int)(count + VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize));
            bool EndingCluster = false;
            if (Pseudocount > Length)
            {
                Pseudocount = (int)(VariousFunctions.DownToNearestCluster(Length - Position, xFile.PartitionInfo.ClusterSize) + VariousFunctions.UpToNearestCluster((Length - Position), xFile.PartitionInfo.ClusterSize));
                RealPseudocount = (int)(Length - Position);
                EndingCluster = true;
            }

            // This int will represent the number of clusters that our data spans
            int ClustersSpanned = (int)ClusterSpanned(Pseudocount);

#if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            for (int i = 0; i < ClustersSpanned && (count - DataRead) > 0; i++)
            {
                // This int will represent the amount of data that we can read in this cluster
                int DataReadableCluster = (int)(xFile.PartitionInfo.ClusterSize - (Position - VariousFunctions.DownToNearestCluster(Position, xFile.PartitionInfo.ClusterSize))).UpToNearest200();
                // This int will represent the amount of data we need to add to the array
                int AddToArray = DataReadableCluster;
                // If that number right above is going to be too much data than we need, let's shrink it down
                if (DataReadableCluster > RealPseudocount - DataRead)
                {
                    DataReadableCluster = (int)VariousFunctions.UpToNearest200(beginningDataToRemove + (RealPseudocount - DataRead));
                    // Looks like we're on the last readthrough!
                    AddToArray = RealPseudocount - DataRead;
                }
                else if (DataReadableCluster > count - DataRead)
                {
                    DataReadableCluster = (int)VariousFunctions.UpToNearest200(beginningDataToRemove + (count - DataRead));
                    // Looks like we're on the last readthrough!
                    AddToArray = count - DataRead;
                }
                else if (i == 0 && Position + DataReadableCluster + AddToArray - beginningDataToRemove > xFile.PartitionInfo.ClusterSize)
                {
                    // Leave datareadablecluster alone, change the addtoarray value
                    AddToArray -= beginningDataToRemove;
                }
                if (AddToArray == 0)
                {
                    break;
                }
                // This array will represent a temp array for holding the data to copy
                // to the array PASSED in the arguments
                byte[] TempData = new byte[DataReadableCluster];
                // Set our IO position
                Underlying.Position = GetRealSectorOffset(xPositionInFile);
                // If we've already read this data...
                if (LastRead200Offset == Underlying.Position && DataReadableCluster <= 0x200)
                {
                    // Save us a disk I/O operation!
                    TempData = LastRead200;
                }
                else
                {
                    // Set the LastRead200Offset
                    LastRead200Offset = (AddToArray <= 0x200) ? Underlying.Position : LastRead200Offset;
                    // Read the data
                    Underlying.Read(TempData, 0, DataReadableCluster);
                    // Set the LastRead200 data
                    if (AddToArray <= 0x200)
                    {
                        Array.Copy(TempData, LastRead200, 0x200);
                    }
                }
#if !DEBUG && !TRACE
                if (xFile.Parent.Name == "FFFE07D1")
                {
                    byte[] TempDebug = new byte[AddToArray];
                    Array.Copy(TempData, beginningDataToRemove, TempDebug, 0, AddToArray);
                    uint CRC = Crc32.Compute(TempDebug);
                    byte[] RealFileBuffer = new byte[AddToArray];
                    FileStream fs = System.IO.File.Open(@"C:\Users\Lander\Desktop\E886364B9B6A4F3A", FileMode.Open);
                    fs.Position = Position;
                    fs.Read(RealFileBuffer, 0, AddToArray);
                    fs.Close();
                    uint RealCRC = Crc32.Compute(RealFileBuffer);
                }
#endif
                // Copy the data we read (or got somehow!) over to the output array
                try
                {
                    Array.Copy(TempData, ((i == 0) ? beginningDataToRemove : 0), array, offset + DataRead, AddToArray);
                }
                catch (Exception e)
                {
                    throw e;
                }
#if !DEBUG && !TRACE
                if (xFile.Parent.Name == "FFFE07D1")
                {
                    byte[] TempDebug = new byte[DataRead];
                    Array.Copy(array, TempDebug, DataRead);
                    uint CRC = Crc32.Compute(TempDebug);
                    FileStream fs = System.IO.File.Open(@"C:\Users\Lander\Desktop\E886364B9B6A4F3A", FileMode.Open);
                    fs.Position = InitialPosition;
                    byte[] RealFileBuffer = new byte[DataRead];
                    fs.Read(RealFileBuffer, 0, DataRead);
                    fs.Close();
                    uint RealCRC = Crc32.Compute(RealFileBuffer);
                }
#endif
                // Increase the DataRead value
                DataRead += AddToArray;
                // Increase the position in the file
                Position = InitialPosition + DataRead;
            }

            Position = InitialPosition + DataRead;
            return DataRead;
        }

        long ClusterOffset
        {
            get
            {
                long cluster = VariousFunctions.DownToNearestCluster(xPositionInFile, xFile.PartitionInfo.ClusterSize);
                // Return the actual block offset + difference
                return VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[DetermineBlockIndex(cluster)], xFile);
            }
        }

        long RealOffset
        {
            get
            {
                // Round the number down to the nearest cluster so that we
                // can easily get the cluster index
                long cluster = VariousFunctions.DownToNearestCluster(xPositionInFile, xFile.PartitionInfo.ClusterSize);
                uint index = (uint)(cluster / xFile.PartitionInfo.ClusterSize);
                // Get the difference so we can add it later...
                long dif = xPositionInFile - cluster;
                cluster = VariousFunctions.GetBlockOffset((uint)xFile.BlocksOccupied[index], xFile) + dif;
                // Return the actual block offset + difference
                return cluster;
            }
        }

        long RealSectorOffset
        {
            get
            {
                return GetRealSectorOffset(xPositionInFile);
            }
        }

        long GetRealSectorOffset(long off)
        {
            // Get the size up to the nearest cluster
            // Divide by cluster size
            // That is the block index.
            long SizeInCluster = VariousFunctions.DownToNearest200(off - VariousFunctions.DownToNearestCluster(off, xFile.PartitionInfo.ClusterSize));//VariousFunctions.GetBlockOffset(xFile.StartingCluster) + 0x4000;            long SizeInCluster = VariousFunctions.DownToNearestCluster(off, xFile.PartitionInfo.ClusterSize) / xFile.PartitionInfo.ClusterSize)
            uint Cluster = (uint)(VariousFunctions.DownToNearestCluster(off, xFile.PartitionInfo.ClusterSize) / xFile.PartitionInfo.ClusterSize);
            //Cluster = (Cluster == 0) ? 0 : Cluster - 1;
            try
            {
                long Underlying = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[Cluster], xFile);
                return Underlying + SizeInCluster;
            }
            catch { return VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[Cluster - 1], xFile); }
        }

        uint DetermineBlockIndex(long Off)
        {
            // Pre-planning... I need to figure ref the rounded offset in order
            // to determine the cluster that this bitch is in
            // So now that we have the rounded number, we can 
            long rounded = VariousFunctions.DownToNearestCluster(Off, xFile.PartitionInfo.ClusterSize);
            // Loop for each cluster, determining if the sizes match
            for (uint i = 0; i < xFile.BlocksOccupied.Length; i++)
            {
                long off = VariousFunctions.GetBlockOffset(xFile.BlocksOccupied[i], xFile);
                if (off == rounded)
                {
                    return i;
                }
            }
            throw new Exception("Block not allocated to this file!");
        }

        // Returns the number of clusters that the value (size?) will span across
        uint ClusterSpanned(long value)
        {
            // Add the cluster size because if we don't, then upon doing this math we
            // will get the actual number - 1
            // EXAMPLE: number = 0x689 or something, and we round it down.  That number
            // is now 0, and 0/x == 0.
            long rounded = ((value % xFile.PartitionInfo.ClusterSize != 0) ? VariousFunctions.DownToNearestCluster(value, xFile.PartitionInfo.ClusterSize) + xFile.PartitionInfo.ClusterSize : value);
            // Divide rounded by cluster size to see how many clusters it spans across...
            return (uint)(rounded / xFile.PartitionInfo.ClusterSize);
        }

        public override void Close()
        {
            Underlying.Close();
            base.Close();
        }

        public override bool CanRead
        {
            get { return Underlying.CanRead; }
        }

        public override bool CanSeek
        {
            get { return Underlying.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return Underlying.CanWrite; }
        }

        public override void Flush()
        {
            Underlying.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
