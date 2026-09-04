using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace All_In_One.Services;

public static class Win32DialogHelper
{
    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint fos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, uint fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
        void GetResults(out IShellItemArray pppenum);
        void GetSelectedItems(out IShellItemArray ppsai);
    }

    [ComImport]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        void BindToHandler(IntPtr pbc, ref Guid rbguid, ref Guid riid, out IntPtr ppvOut);
        void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
        void GetPropertyDescriptionList(ref Guid keyType, ref Guid riid, out IntPtr ppv);
        void GetAttributes(int AttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
        void GetCount(out uint pdwNumItems);
        void GetItemAt(uint dwIndex, out IShellItem ppsi);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid rbguid, ref Guid riid, out IntPtr ppvOut);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialogRC { }

    private const uint FOS_ALLOWMULTISELECT = 0x00000200;
    private const uint FOS_PICKFOLDERS = 0x00000020;
    private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    public static List<string> PickFiles(IntPtr hwnd)
    {
        List<string> result = [];
        IFileOpenDialog dialog = (IFileOpenDialog) new FileOpenDialogRC();

        dialog.GetOptions(out uint options);
        dialog.SetOptions(options | FOS_ALLOWMULTISELECT | FOS_FORCEFILESYSTEM);

        if (dialog.Show(hwnd) == 0)
        {
            dialog.GetResults(out IShellItemArray? array);
            array.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                array.GetItemAt(i, out IShellItem? item);
                item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                if (!string.IsNullOrEmpty(path))
                {
                    result.Add(path);
                }
            }
        }
        return result;
    }

    public static string? PickFolder(IntPtr hwnd)
    {
        IFileOpenDialog dialog = (IFileOpenDialog) new FileOpenDialogRC();

        dialog.GetOptions(out uint options);
        dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);

        if (dialog.Show(hwnd) == 0)
        {
            dialog.GetResult(out IShellItem? item);
            item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
            return path;
        }
        return null;
    }
}