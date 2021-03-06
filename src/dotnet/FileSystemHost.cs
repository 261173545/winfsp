/*
 * dotnet/FileSystemHost.cs
 *
 * Copyright 2015-2018 Bill Zissimopoulos
 */
/*
 * This file is part of WinFsp.
 *
 * You can redistribute it and/or modify it under the terms of the GNU
 * General Public License version 3 as published by the Free Software
 * Foundation.
 *
 * Licensees holding a valid commercial license may use this software
 * in accordance with the commercial license agreement provided in
 * conjunction with the software.  The terms and conditions of any such
 * commercial license agreement shall govern, supersede, and render
 * ineffective any application of the GPLv3 license to this software,
 * notwithstanding of any reference thereto in the software or
 * associated repository.
 */

using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

using Fsp.Interop;

namespace Fsp
{

    /// <summary>
    /// Provides a means to host (mount) a file system.
    /// </summary>
    public class FileSystemHost : IDisposable
    {
        /* ctor/dtor */
        /// <summary>
        /// Creates an instance of the FileSystemHost class.
        /// </summary>
        /// <param name="FileSystem">The file system to host.</param>
        public FileSystemHost(FileSystemBase FileSystem)
        {
            _VolumeParams.Flags = VolumeParams.UmFileContextIsFullContext;
            _FileSystem = FileSystem;
        }
        ~FileSystemHost()
        {
            Dispose(false);
        }
        /// <summary>
        /// Unmounts the file system and releases all associated resources.
        /// </summary>
        public void Dispose()
        {
            lock (this)
                Dispose(true);
            GC.SuppressFinalize(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (IntPtr.Zero != _FileSystemPtr)
            {
                Api.FspFileSystemStopDispatcher(_FileSystemPtr);
                if (disposing)
                    try
                    {
                        _FileSystem.Unmounted(this);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler(_FileSystem, ex);
                    }
                Api.DisposeUserContext(_FileSystemPtr);
                Api.FspFileSystemDelete(_FileSystemPtr);
                _FileSystemPtr = IntPtr.Zero;
            }
        }

        /* properties */
        /// <summary>
        /// Gets or sets the sector size used by the file system.
        /// </summary>
        public UInt16 SectorSize
        {
            get { return _VolumeParams.SectorSize; }
            set { _VolumeParams.SectorSize = value; }
        }
        /// <summary>
        /// Gets or sets the sectors per allocation unit used by the file system.
        /// </summary>
        public UInt16 SectorsPerAllocationUnit
        {
            get { return _VolumeParams.SectorsPerAllocationUnit; }
            set { _VolumeParams.SectorsPerAllocationUnit = value; }
        }
        /// <summary>
        /// Gets or sets the maximum path component length used by the file system.
        /// </summary>
        public UInt16 MaxComponentLength
        {
            get { return _VolumeParams.MaxComponentLength; }
            set { _VolumeParams.MaxComponentLength = value; }
        }
        /// <summary>
        /// Gets or sets the volume creation time.
        /// </summary>
        public UInt64 VolumeCreationTime
        {
            get { return _VolumeParams.VolumeCreationTime; }
            set { _VolumeParams.VolumeCreationTime = value; }
        }
        /// <summary>
        /// Gets or sets the volume serial number.
        /// </summary>
        public UInt32 VolumeSerialNumber
        {
            get { return _VolumeParams.VolumeSerialNumber; }
            set { _VolumeParams.VolumeSerialNumber = value; }
        }
        /// <summary>
        /// Gets or sets the file information timeout.
        /// </summary>
        public UInt32 FileInfoTimeout
        {
            get { return _VolumeParams.FileInfoTimeout; }
            set { _VolumeParams.FileInfoTimeout = value; }
        }
        /// <summary>
        /// Gets or sets a value that determines whether the file system is case sensitive.
        /// </summary>
        public Boolean CaseSensitiveSearch
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.CaseSensitiveSearch); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.CaseSensitiveSearch : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether a case insensitive file system
        /// preserves case in file names.
        /// </summary>
        public Boolean CasePreservedNames
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.CasePreservedNames); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.CasePreservedNames : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether file names support unicode characters.
        /// </summary>
        public Boolean UnicodeOnDisk
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.UnicodeOnDisk); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.UnicodeOnDisk : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether the file system supports ACL security.
        /// </summary>
        public Boolean PersistentAcls
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.PersistentAcls); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.PersistentAcls : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether the file system supports reparse points.
        /// </summary>
        public Boolean ReparsePoints
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.ReparsePoints); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.ReparsePoints : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether the file system allows creation of
        /// symbolic links without additional privileges.
        /// </summary>
        public Boolean ReparsePointsAccessCheck
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.ReparsePointsAccessCheck); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.ReparsePointsAccessCheck : 0); }
        }
        /// <summary>
        /// Gets or sets a value that determines whether the file system supports named streams.
        /// </summary>
        public Boolean NamedStreams
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.NamedStreams); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.NamedStreams : 0); }
        }
        public Boolean PostCleanupWhenModifiedOnly
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.PostCleanupWhenModifiedOnly); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.PostCleanupWhenModifiedOnly : 0); }
        }
        public Boolean PassQueryDirectoryPattern
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.PassQueryDirectoryPattern); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.PassQueryDirectoryPattern : 0); }
        }
        public Boolean PassQueryDirectoryFileName
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.PassQueryDirectoryFileName); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.PassQueryDirectoryFileName : 0); }
        }
        public Boolean FlushAndPurgeOnCleanup
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.FlushAndPurgeOnCleanup); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.FlushAndPurgeOnCleanup : 0); }
        }
        public Boolean DeviceControl
        {
            get { return 0 != (_VolumeParams.Flags & VolumeParams.DeviceControl); }
            set { _VolumeParams.Flags |= (value ? VolumeParams.DeviceControl : 0); }
        }
        /// <summary>
        /// Gets or sets the prefix for a network file system.
        /// </summary>
        public String Prefix
        {
            get { return _VolumeParams.GetPrefix(); }
            set {  _VolumeParams.SetPrefix(value); }
        }
        /// <summary>
        /// Gets or sets the file system name.
        /// </summary>
        public String FileSystemName
        {
            get { return _VolumeParams.GetFileSystemName(); }
            set {  _VolumeParams.SetFileSystemName(value); }
        }

        /* control */
        /// <summary>
        /// Checks whether mounting a file system is possible.
        /// </summary>
        /// <param name="MountPoint">
        /// The mount point for the new file system. A value of null means that
        /// the file system should use the next available drive letter counting
        /// downwards from Z: as its mount point.
        /// </param>
        /// <returns>STATUS_SUCCESS or error code.</returns>
        public Int32 Preflight(String MountPoint)
        {
            return Api.FspFileSystemPreflight(
                _VolumeParams.IsPrefixEmpty() ? "WinFsp.Disk" : "WinFsp.Net",
                MountPoint);
        }
        /// <summary>
        /// Mounts a file system.
        /// </summary>
        /// <param name="MountPoint">
        /// The mount point for the new file system. A value of null means that
        /// the file system should use the next available drive letter counting
        /// downwards from Z: as its mount point.
        /// </param>
        /// <param name="SecurityDescriptor">
        /// Security descriptor to use if mounting on (newly created) directory.
        /// A value of null means the directory should be created with default
        /// security.
        /// </param>
        /// <param name="Synchronized">
        /// If true file system operations are synchronized using an exclusive lock.
        /// </param>
        /// <param name="DebugLog">
        /// A value of 0 disables all debug logging.
        /// A value of -1 enables all debug logging.
        /// </param>
        /// <returns></returns>
        public Int32 Mount(String MountPoint,
            Byte[] SecurityDescriptor = null,
            Boolean Synchronized = false,
            UInt32 DebugLog = 0)
        {
            Int32 Result;
            try
            {
                Result = _FileSystem.Init(this);
            }
            catch (Exception ex)
            {
                Result = ExceptionHandler(_FileSystem, ex);
            }
            if (0 > Result)
                return Result;
            Result = Api.FspFileSystemCreate(
                _VolumeParams.IsPrefixEmpty() ? "WinFsp.Disk" : "WinFsp.Net",
                ref _VolumeParams, _FileSystemInterfacePtr, out _FileSystemPtr);
            if (0 > Result)
                return Result;
            Api.SetUserContext(_FileSystemPtr, _FileSystem);
            Api.FspFileSystemSetOperationGuardStrategy(_FileSystemPtr, Synchronized ?
                1/*FSP_FILE_SYSTEM_OPERATION_GUARD_STRATEGY_COARSE*/ :
                0/*FSP_FILE_SYSTEM_OPERATION_GUARD_STRATEGY_FINE*/);
            Api.FspFileSystemSetDebugLog(_FileSystemPtr, DebugLog);
            Result = Api.FspFileSystemSetMountPointEx(_FileSystemPtr, MountPoint,
                SecurityDescriptor);
            if (0 <= Result)
            {
                try
                {
                    Result = _FileSystem.Mounted(this);
                }
                catch (Exception ex)
                {
                    Result = ExceptionHandler(_FileSystem, ex);
                }
                if (0 <= Result)
                {
                    Result = Api.FspFileSystemStartDispatcher(_FileSystemPtr, 0);
                    if (0 > Result)
                        try
                        {
                            _FileSystem.Unmounted(this);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandler(_FileSystem, ex);
                        }
                }
            }
            if (0 > Result)
            {
                Api.DisposeUserContext(_FileSystemPtr);
                Api.FspFileSystemDelete(_FileSystemPtr);
                _FileSystemPtr = IntPtr.Zero;
            }
            return Result;
        }
        /// <summary>
        /// Unmounts the file system and releases all associated resources.
        /// </summary>
        public void Unmount()
        {
            Dispose();
        }
        /// <summary>
        /// Gets the file system mount point.
        /// </summary>
        /// <returns>The file system mount point.</returns>
        public String MountPoint()
        {
            return IntPtr.Zero != _FileSystemPtr ?
                Marshal.PtrToStringUni(Api.FspFileSystemMountPoint(_FileSystemPtr)) : null;
        }
        public IntPtr FileSystemHandle()
        {
            return _FileSystemPtr;
        }
        /// <summary>
        /// Gets the hosted file system.
        /// </summary>
        /// <returns>The hosted file system.</returns>
        public FileSystemBase FileSystem()
        {
            return _FileSystem;
        }
        /// <summary>
        /// Sets the debug log file to use when debug logging is enabled.
        /// </summary>
        /// <param name="FileName">
        /// The debug log file name. A value of "-" means standard error output.
        /// </param>
        /// <returns>STATUS_SUCCESS or error code.</returns>
        public static Int32 SetDebugLogFile(String FileName)
        {
            return Api.SetDebugLogFile(FileName);
        }
        /// <summary>
        /// Return the installed version of WinFsp.
        /// </summary>
        public static Version Version()
        {
            return Api.GetVersion();
        }

        /* FSP_FILE_SYSTEM_INTERFACE */
        private static Byte[] ByteBufferNotNull = new Byte[0];
        private static Int32 ExceptionHandler(
            FileSystemBase FileSystem,
            Exception ex)
        {
            try
            {
                return FileSystem.ExceptionHandler(ex);
            }
            catch
            {
                return unchecked((Int32)0xc00000e9)/*STATUS_UNEXPECTED_IO_ERROR*/;
            }
        }
        private static Int32 GetVolumeInfo(
            IntPtr FileSystemPtr,
            out VolumeInfo VolumeInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                return FileSystem.GetVolumeInfo(
                    out VolumeInfo);
            }
            catch (Exception ex)
            {
                VolumeInfo = default(VolumeInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetVolumeLabel(
            IntPtr FileSystemPtr,
            String VolumeLabel,
            out VolumeInfo VolumeInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                return FileSystem.SetVolumeLabel(
                    VolumeLabel,
                    out VolumeInfo);
            }
            catch (Exception ex)
            {
                VolumeInfo = default(VolumeInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetSecurityByName(
            IntPtr FileSystemPtr,
            String FileName,
            IntPtr PFileAttributes/* or ReparsePointIndex */,
            IntPtr SecurityDescriptor,
            IntPtr PSecurityDescriptorSize)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                UInt32 FileAttributes;
                Byte[] SecurityDescriptorBytes = null;
                Int32 Result;
                if (IntPtr.Zero != PSecurityDescriptorSize)
                    SecurityDescriptorBytes = ByteBufferNotNull;
                Result = FileSystem.GetSecurityByName(
                    FileName,
                    out FileAttributes,
                    ref SecurityDescriptorBytes);
                if (0 <= Result && 260/*STATUS_REPARSE*/ != Result)
                {
                    if (IntPtr.Zero != PFileAttributes)
                        Marshal.WriteInt32(PFileAttributes, (Int32)FileAttributes);
                    Result = Api.CopySecurityDescriptor(SecurityDescriptorBytes,
                        SecurityDescriptor, PSecurityDescriptorSize);
                }
                return Result;
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Create(
            IntPtr FileSystemPtr,
            String FileName,
            UInt32 CreateOptions,
            UInt32 GrantedAccess,
            UInt32 FileAttributes,
            IntPtr SecurityDescriptor,
            UInt64 AllocationSize,
            ref FullContext FullContext,
            ref OpenFileInfo OpenFileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                String NormalizedName;
                Int32 Result;
                Result = FileSystem.Create(
                    FileName,
                    CreateOptions,
                    GrantedAccess,
                    FileAttributes,
                    Api.MakeSecurityDescriptor(SecurityDescriptor),
                    AllocationSize,
                    out FileNode,
                    out FileDesc,
                    out OpenFileInfo.FileInfo,
                    out NormalizedName);
                if (0 <= Result)
                {
                    if (null != NormalizedName)
                        OpenFileInfo.SetNormalizedName(NormalizedName);
                    Api.SetFullContext(ref FullContext, FileNode, FileDesc);
                }
                return Result;
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Open(
            IntPtr FileSystemPtr,
            String FileName,
            UInt32 CreateOptions,
            UInt32 GrantedAccess,
            ref FullContext FullContext,
            ref OpenFileInfo OpenFileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                String NormalizedName;
                Int32 Result;
                Result = FileSystem.Open(
                    FileName,
                    CreateOptions,
                    GrantedAccess,
                    out FileNode,
                    out FileDesc,
                    out OpenFileInfo.FileInfo,
                    out NormalizedName);
                if (0 <= Result)
                {
                    if (null != NormalizedName)
                        OpenFileInfo.SetNormalizedName(NormalizedName);
                    Api.SetFullContext(ref FullContext, FileNode, FileDesc);
                }
                return Result;
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Overwrite(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            UInt32 FileAttributes,
            Boolean ReplaceFileAttributes,
            UInt64 AllocationSize,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Overwrite(
                    FileNode,
                    FileDesc,
                    FileAttributes,
                    ReplaceFileAttributes,
                    AllocationSize,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static void Cleanup(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            UInt32 Flags)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                FileSystem.Cleanup(
                    FileNode,
                    FileDesc,
                    FileName,
                    Flags);
            }
            catch (Exception ex)
            {
                ExceptionHandler(FileSystem, ex);
            }
        }
        private static void Close(
            IntPtr FileSystemPtr,
            ref FullContext FullContext)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                FileSystem.Close(
                    FileNode,
                    FileDesc);
                Api.DisposeFullContext(ref FullContext);
            }
            catch (Exception ex)
            {
                ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Read(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            IntPtr Buffer,
            UInt64 Offset,
            UInt32 Length,
            out UInt32 PBytesTransferred)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Read(
                    FileNode,
                    FileDesc,
                    Buffer,
                    Offset,
                    Length,
                    out PBytesTransferred);
            }
            catch (Exception ex)
            {
                PBytesTransferred = default(UInt32);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Write(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            IntPtr Buffer,
            UInt64 Offset,
            UInt32 Length,
            Boolean WriteToEndOfFile,
            Boolean ConstrainedIo,
            out UInt32 PBytesTransferred,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Write(
                    FileNode,
                    FileDesc,
                    Buffer,
                    Offset,
                    Length,
                    WriteToEndOfFile,
                    ConstrainedIo,
                    out PBytesTransferred,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                PBytesTransferred = default(UInt32);
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Flush(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Flush(
                    FileNode,
                    FileDesc,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetFileInfo(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.GetFileInfo(
                    FileNode,
                    FileDesc,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetBasicInfo(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            UInt32 FileAttributes,
            UInt64 CreationTime,
            UInt64 LastAccessTime,
            UInt64 LastWriteTime,
            UInt64 ChangeTime,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.SetBasicInfo(
                    FileNode,
                    FileDesc,
                    FileAttributes,
                    CreationTime,
                    LastAccessTime,
                    LastWriteTime,
                    ChangeTime,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetFileSize(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            UInt64 NewSize,
            Boolean SetAllocationSize,
            out FileInfo FileInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.SetFileSize(
                    FileNode,
                    FileDesc,
                    NewSize,
                    SetAllocationSize,
                    out FileInfo);
            }
            catch (Exception ex)
            {
                FileInfo = default(FileInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Rename(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            String NewFileName,
            Boolean ReplaceIfExists)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Rename(
                    FileNode,
                    FileDesc,
                    FileName,
                    NewFileName,
                    ReplaceIfExists);
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetSecurity(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            IntPtr SecurityDescriptor,
            IntPtr PSecurityDescriptorSize)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Byte[] SecurityDescriptorBytes;
                Int32 Result;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                SecurityDescriptorBytes = ByteBufferNotNull;
                Result = FileSystem.GetSecurity(
                    FileNode,
                    FileDesc,
                    ref SecurityDescriptorBytes);
                if (0 <= Result)
                    Result = Api.CopySecurityDescriptor(SecurityDescriptorBytes,
                        SecurityDescriptor, PSecurityDescriptorSize);
                return Result;
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetSecurity(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            UInt32 SecurityInformation,
            IntPtr ModificationDescriptor)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                AccessControlSections Sections;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                Sections = AccessControlSections.None;
                if (0 != (SecurityInformation & 1/*OWNER_SECURITY_INFORMATION*/))
                    Sections |= AccessControlSections.Owner;
                if (0 != (SecurityInformation & 2/*GROUP_SECURITY_INFORMATION*/))
                    Sections |= AccessControlSections.Group;
                if (0 != (SecurityInformation & 4/*DACL_SECURITY_INFORMATION*/))
                    Sections |= AccessControlSections.Access;
                if (0 != (SecurityInformation & 8/*SACL_SECURITY_INFORMATION*/))
                    Sections |= AccessControlSections.Audit;
                return FileSystem.SetSecurity(
                    FileNode,
                    FileDesc,
                    Sections,
                    Api.MakeSecurityDescriptor(ModificationDescriptor));
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 ReadDirectory(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String Pattern,
            String Marker,
            IntPtr Buffer,
            UInt32 Length,
            out UInt32 PBytesTransferred)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.ReadDirectory(
                    FileNode,
                    FileDesc,
                    Pattern,
                    Marker,
                    Buffer,
                    Length,
                    out PBytesTransferred);
            }
            catch (Exception ex)
            {
                PBytesTransferred = default(UInt32);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 ResolveReparsePoints(
            IntPtr FileSystemPtr,
            String FileName,
            UInt32 ReparsePointIndex,
            Boolean ResolveLastPathComponent,
            out IoStatusBlock PIoStatus,
            IntPtr Buffer,
            IntPtr PSize)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                return FileSystem.ResolveReparsePoints(
                    FileName,
                    ReparsePointIndex,
                    ResolveLastPathComponent,
                    out PIoStatus,
                    Buffer,
                    PSize);
            }
            catch (Exception ex)
            {
                PIoStatus = default(IoStatusBlock);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetReparsePoint(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            IntPtr Buffer,
            IntPtr PSize)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Byte[] ReparseData;
                Object FileNode, FileDesc;
                Int32 Result;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                ReparseData = null;
                Result = FileSystem.GetReparsePoint(
                    FileNode,
                    FileDesc,
                    FileName,
                    ref ReparseData);
                if (0 <= Result)
                    Result = Api.CopyReparsePoint(ReparseData, Buffer, PSize);
                return Result;
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetReparsePoint(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            IntPtr Buffer,
            UIntPtr Size)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.SetReparsePoint(
                    FileNode,
                    FileDesc,
                    FileName,
                    Api.MakeReparsePoint(Buffer, Size));
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 DeleteReparsePoint(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            IntPtr Buffer,
            UIntPtr Size)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.DeleteReparsePoint(
                    FileNode,
                    FileDesc,
                    FileName,
                    Api.MakeReparsePoint(Buffer, Size));
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetStreamInfo(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            IntPtr Buffer,
            UInt32 Length,
            out UInt32 PBytesTransferred)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.GetStreamInfo(
                    FileNode,
                    FileDesc,
                    Buffer,
                    Length,
                    out PBytesTransferred);
            }
            catch (Exception ex)
            {
                PBytesTransferred = default(UInt32);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 GetDirInfoByName(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            out DirInfo DirInfo)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                String NormalizedName;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                DirInfo = default(DirInfo);
                Int32 Result = FileSystem.GetDirInfoByName(
                    FileNode,
                    FileDesc,
                    FileName,
                    out NormalizedName,
                    out DirInfo.FileInfo);
                DirInfo.SetFileNameBuf(NormalizedName);
                return Result;
            }
            catch (Exception ex)
            {
                DirInfo = default(DirInfo);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 Control(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            UInt32 ControlCode,
            IntPtr InputBuffer, UInt32 InputBufferLength,
            IntPtr OutputBuffer, UInt32 OutputBufferLength,
            out UInt32 PBytesTransferred)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.Control(
                    FileNode,
                    FileDesc,
                    ControlCode,
                    InputBuffer,
                    InputBufferLength,
                    OutputBuffer,
                    OutputBufferLength,
                    out PBytesTransferred);
            }
            catch (Exception ex)
            {
                PBytesTransferred = default(UInt32);
                return ExceptionHandler(FileSystem, ex);
            }
        }
        private static Int32 SetDelete(
            IntPtr FileSystemPtr,
            ref FullContext FullContext,
            String FileName,
            Boolean DeleteFile)
        {
            FileSystemBase FileSystem = (FileSystemBase)Api.GetUserContext(FileSystemPtr);
            try
            {
                Object FileNode, FileDesc;
                Api.GetFullContext(ref FullContext, out FileNode, out FileDesc);
                return FileSystem.SetDelete(
                    FileNode,
                    FileDesc,
                    FileName,
                    DeleteFile);
            }
            catch (Exception ex)
            {
                return ExceptionHandler(FileSystem, ex);
            }
        }

        static FileSystemHost()
        {
            _FileSystemInterface.GetVolumeInfo = GetVolumeInfo;
            _FileSystemInterface.SetVolumeLabel = SetVolumeLabel;
            _FileSystemInterface.GetSecurityByName = GetSecurityByName;
            _FileSystemInterface.Create = Create;
            _FileSystemInterface.Open = Open;
            _FileSystemInterface.Overwrite = Overwrite;
            _FileSystemInterface.Cleanup = Cleanup;
            _FileSystemInterface.Close = Close;
            _FileSystemInterface.Read = Read;
            _FileSystemInterface.Write = Write;
            _FileSystemInterface.Flush = Flush;
            _FileSystemInterface.GetFileInfo = GetFileInfo;
            _FileSystemInterface.SetBasicInfo = SetBasicInfo;
            _FileSystemInterface.SetFileSize = SetFileSize;
            _FileSystemInterface.Rename = Rename;
            _FileSystemInterface.GetSecurity = GetSecurity;
            _FileSystemInterface.SetSecurity = SetSecurity;
            _FileSystemInterface.ReadDirectory = ReadDirectory;
            _FileSystemInterface.ResolveReparsePoints = ResolveReparsePoints;
            _FileSystemInterface.GetReparsePoint = GetReparsePoint;
            _FileSystemInterface.SetReparsePoint = SetReparsePoint;
            _FileSystemInterface.DeleteReparsePoint = DeleteReparsePoint;
            _FileSystemInterface.GetStreamInfo = GetStreamInfo;
            _FileSystemInterface.GetDirInfoByName = GetDirInfoByName;
            _FileSystemInterface.Control = Control;
            _FileSystemInterface.SetDelete = SetDelete;

            _FileSystemInterfacePtr = Marshal.AllocHGlobal(FileSystemInterface.Size);
            Marshal.StructureToPtr(_FileSystemInterface, _FileSystemInterfacePtr, false);
        }

        private static FileSystemInterface _FileSystemInterface;
        private static IntPtr _FileSystemInterfacePtr;
        private VolumeParams _VolumeParams;
        private FileSystemBase _FileSystem;
        private IntPtr _FileSystemPtr;
    }

}
