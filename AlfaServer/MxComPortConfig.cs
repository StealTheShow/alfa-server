using System;
using System.Runtime.InteropServices;

namespace AlfaServer
{
    public class MxComPortConfig
    {
        private const uint MoxaSetOpMode = (0x400 + 66);
        private const uint MoxaGetOpMode = (0x400 + 67);
        private const uint Rs232Mode = 0;
        private const uint Rs4852WireMode = 1;
        private const uint Rs422Mode = 2;
        private const uint Rs4854WireMode = 3;
        private const UInt32 OpenExisting = 3;
        private const UInt32 GenericRead = 0x80000000;
        private const UInt32 GenericWrite = 0x40000000;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        private readonly String _sPort;

        public MxComPortConfig(String sPort)
        {
            this._sPort = sPort;
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW",
            SetLastError = true)]
        private static extern IntPtr CreateFileW(string lpFileName,
                                                 UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr
                                                                                                 lpSecurityAttributes,
                                                 UInt32 dwCreationDisposition, UInt32
                                                                                   dwFlagsAndAttributes,
                                                 IntPtr hTemplateFile);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle",
            SetLastError = true)]
        private static extern Int32 CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl",
            SetLastError = true)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            ref byte lpInBuffer,
            int nInBufferSize,
            ref byte lpOutBuffer,
            int nOutBufferSize,
            ref int lpBytesReturned,
            IntPtr lpOverlapped
            );

        private bool SetComPortInterface(uint mode)
        {
            IntPtr comPort = CreateFileW(_sPort, GenericRead, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (comPort == InvalidHandleValue)
                return false;
            int nBytesReturned = 0;
            var bIn = (byte) mode;
            byte bOut = 0;
            DeviceIoControl(comPort, MoxaSetOpMode, ref bIn, 1, ref bOut, 0, ref nBytesReturned, IntPtr.Zero);
            CloseHandle(comPort);
            return true;
        }

        public bool SetRs232()
        {
            return SetComPortInterface(Rs232Mode);
        }

        public bool SetRs422()
        {
            return SetComPortInterface(Rs422Mode);
        }

        public bool SetRs485TwoWire()
        {
            return SetComPortInterface(Rs4852WireMode);
        }

        public bool SetRs485FourWire()
        {
            return SetComPortInterface(Rs4854WireMode);
        }
    }
}