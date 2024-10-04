using System;
using System.Runtime.InteropServices;
using System.Text;
using static SetupAPI;

public class SetupAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    public enum RegPropertyType : uint
    {
        SPDRP_DEVICEDESC = 0x00000000,  

    }

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid,
        string Enumerator,
        IntPtr hwndParent,
        int Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInfo(
        IntPtr hDeviceInfoSet,
        int MemberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr hDeviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        RegPropertyType Property,
        out uint PropertyRegDataType,
        StringBuilder PropertyBuffer,
        uint PropertyBufferSize,
        ref int RequiredSize);

    [DllImport("cfgmgr32.dll", SetLastError = true)]
    public static extern uint CM_Request_Device_Eject(
        uint dnDevInst,
        IntPtr pDeviceInterfaceClassGuid,
        uint ulFlags,
        IntPtr pExclusionList,
        uint dwTimeout,
        IntPtr pContext);
}

class Program
{
    public static string GetRegistryProperty(IntPtr PnPHandle, ref SetupAPI.SP_DEVINFO_DATA DeviceInfoData, SetupAPI.RegPropertyType Property)
    {
        int RequiredSize = 0;
        uint PropertyRegDataType;
        StringBuilder Buffer = new StringBuilder(1024);  

        bool result = SetupAPI.SetupDiGetDeviceRegistryProperty(
            PnPHandle,
            ref DeviceInfoData,
            Property,
            out PropertyRegDataType,
            Buffer,
            (uint)Buffer.Capacity,
            ref RequiredSize
        );

        if (!result)
        {
            Console.WriteLine("Ошибка получения свойства устройства: " + Marshal.GetLastWin32Error());
            return null;
        }

        return Buffer.ToString();
    }

    static void Main(string[] args)
    {
        Guid usbGuid = new Guid("{36fc9e60-c465-11cf-8056-444553540000}");  // GUID для USB
        IntPtr usbDescriptor = SetupAPI.SetupDiGetClassDevs(ref usbGuid, null, IntPtr.Zero, 0x00000002);

        if (usbDescriptor == IntPtr.Zero)
        {
            Console.WriteLine("Ошибка получения дескриптора устройств: " + Marshal.GetLastWin32Error());
            return;
        }

        SetupAPI.SP_DEVINFO_DATA deviceInfoData = new SetupAPI.SP_DEVINFO_DATA();
        deviceInfoData.cbSize = Marshal.SizeOf(typeof(SetupAPI.SP_DEVINFO_DATA));

        int i = 0;

        while (SetupAPI.SetupDiEnumDeviceInfo(usbDescriptor, i, ref deviceInfoData))
        {
            string deviceDescription = GetRegistryProperty(usbDescriptor, ref deviceInfoData, SetupAPI.RegPropertyType.SPDRP_DEVICEDESC);
            if (!string.IsNullOrEmpty(deviceDescription))
            {
                Console.WriteLine($"{i} Устройство: {deviceDescription}");

                // Попробуем безопасно отключить устройство
                uint result = SetupAPI.CM_Request_Device_Eject(deviceInfoData.DevInst, IntPtr.Zero, 0, IntPtr.Zero, 0, IntPtr.Zero);

                if (result == 0)
                {
                    Console.WriteLine("Устройство успешно отключено.");
                }
                else
                {
                    Console.WriteLine("Ошибка отключения устройства.");
                }
            }

            i++;
        }
    }
}
