using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PushServiceConsole
{
    /// <summary>
    /// CharSet Ansi
    /// sizeconst 256
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PacketDTO
    {
        public byte STX;
        public int COMMAND;
        public int HEADER_LENGTH;
        public int DATA_LENGTH;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string HEADER;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 210)]
        public string DATA;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string AUTH;
        public byte ETX;
        public PacketDTO(byte stx, int command, int headerLength, int dataLength, string header, string data, string auth, byte etx)
        {
            this.STX = stx;
            this.COMMAND = command;
            this.HEADER_LENGTH = headerLength;
            this.DATA_LENGTH = dataLength;
            this.HEADER = header;
            this.DATA = data;
            this.AUTH = auth;
            this.ETX = etx;
        }
    }

    public class PacketConvert
    {
        /// <summary>
        /// 1. 구조체에 할당된 메모리의 크기를 구한다.
        /// 2. 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.
        /// 3. 할당된 구조체 객체의 주소를 구한다.
        /// 4. 구조체 객체를 배열에 복사
        /// 5. 비관리 메모리 영역에 할당했던 메모리를 해제함
        /// 6. 배열을 리턴
        /// </summary>
        /// <param name="obj"></param>
        public static byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(datasize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] data = new byte[datasize];
            Marshal.Copy(buff, data, 0, datasize);
            Marshal.FreeHGlobal(buff);
            return data;
        }

        /// <summary>
        /// 1. 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.
        /// 2. 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.
        /// 3. 복사된 데이터를 구조체 객체로 변환한다.
        /// 4. 비관리 메모리 영역에 할당했던 메모리를 해제함.
        /// 5. 구조체와 원래의 데이터의 크기 비교
        /// 6. 크기가 다르면 null 리턴
        /// 7. 구조체 리턴
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        public static object ByteToStructure(byte[] data, Type type)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);
            if (Marshal.SizeOf(obj) != data.Length) return null;
            return obj;
        }
    }
}