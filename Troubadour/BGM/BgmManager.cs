using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace Troubadour.BGM;

public class BgmManager
{
    private readonly ISigScanner sigScanner;

    private IntPtr baseAddressPtr;
    private IntPtr bgmManagerPtr;
    private IntPtr bgmManager;
    private unsafe BgmScene* scenes;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ResetBgmContextDelegate(IntPtr bgmManager);
    private ResetBgmContextDelegate? bgmRestart;

    private static readonly string BaseAddressSignature = "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 51 83 78 08 0B";
    private static readonly string ResetBgmContextSignature = "E8 ?? ?? ?? ?? 88 9E ?? ?? ?? ?? 84 DB";
    private static readonly string MusicManagerSignature = "48 8B 8F ?? ?? ?? ?? 39 70 20 0F 94 C2 45 33 C0";

    public const ushort HIGHEST_PRIORITY = 0;
    public const ushort LOWEST_PRIORITY = 11;

    public BgmManager(ISigScanner sigScanner)
    {
        this.sigScanner = sigScanner ?? throw new ArgumentNullException(nameof(sigScanner));
    }

    /// <summary>
    /// Initializes the BGM manager and resolves required signatures and pointers.
    /// </summary>
    public unsafe void Initialize()
    {
        try
        {
            if (sigScanner == null)
            {
                throw new Exception("SigScanner must be properly injected.");
            }

            baseAddressPtr = sigScanner.GetStaticAddressFromSig(BaseAddressSignature);
            if (baseAddressPtr == IntPtr.Zero)
            {
                throw new Exception("Base address not found.");
            }

            var resetBgmContextPtr = sigScanner.ScanText(ResetBgmContextSignature);
            if (resetBgmContextPtr == IntPtr.Zero)
            {
                throw new Exception("Unable to locate resetBgmContextPtr signature.");
            }

            bgmRestart = Marshal.GetDelegateForFunctionPointer<ResetBgmContextDelegate>(resetBgmContextPtr);

            var bgmLocationPtr = sigScanner.ScanText(MusicManagerSignature);
            if (bgmLocationPtr == IntPtr.Zero)
            {
                throw new Exception("MusicManager signature not found.");
            }

            bgmManager = Marshal.ReadIntPtr(new IntPtr(Framework.Instance()) + Marshal.ReadInt32(bgmLocationPtr + 0x3));

            InitScenes();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error initializing signatures: {ex.Message}");
        }
    }

    /// /// <summary>
    /// Initializes the BGM scenes.
    /// </summary>
    private unsafe void InitScenes()
    {
        if (bgmRestart == null)
        {
            throw new Exception("ResetBgmContext function not initialized.");
        }

        bgmManagerPtr = Marshal.ReadIntPtr(baseAddressPtr);
        if (bgmManagerPtr == IntPtr.Zero)
        {
            throw new Exception("Base object pointer is invalid.");
        }

        var bgmSceneListPtr = Marshal.ReadIntPtr(bgmManagerPtr + 0xC0);
        if (bgmSceneListPtr == IntPtr.Zero)
        {
            throw new Exception("BGM Scene List pointer is invalid.");
        }

        scenes = (BgmScene*)bgmSceneListPtr.ToPointer();
        if (scenes == null)
        {
            throw new Exception("Failed to access BGM scenes.");
        }
    }

    /// <summary>
    /// Retrieves the pointer to the BGM scenes.
    /// </summary>
    /// <returns>A pointer to the BGM scenes.</returns>
    public unsafe BgmScene* GetScenes()
    {
        return scenes;
    }

    /// <summary>
    /// Retrieves the currently playing BGM ID, skipping the highest priority scene.
    /// </summary>
    /// <returns>The ID of the currently playing BGM, or 0 if none is playing.</returns>
    public unsafe ushort GetCurrentBgmId()
    {
        var scenes = GetScenes();
        if (scenes == null)
        {
            return 0;
        }

        for (ushort i = (HIGHEST_PRIORITY + 1); i <= LOWEST_PRIORITY; i++)
        {
            if (scenes[i].BgmId != 0)
            {
                return scenes[i].BgmId;
            }
        }

        return 0;
    }

    /// <summary>
    /// Plays a specified BGM at the given priority level.
    /// </summary>
    /// <param name="priority">The priority level.</param>
    /// <param name="bgmId">The ID of the BGM to play.</param>
    /// <param name="bgmReference">The reference ID of the BGM.</param>
    /// <param name="sceneFlags">The scene flags to use.</param>
    /// <param name="timer">The timer for the BGM.</param>
    public unsafe void Play(ushort priority, ushort bgmId, ushort bgmReference, SceneFlags sceneFlags, int timer = 0)
    {
        var scenes = GetScenes();
        if (scenes == null)
        {
            return;
        }

        if (priority > LOWEST_PRIORITY)
        {
            return;
        }

        scenes[priority].BgmId = bgmId;
        scenes[priority].BgmReference = bgmReference;
        scenes[priority].Flags = sceneFlags;
        scenes[priority].Timer = timer;
    }

    /// <summary>
    /// Stops the BGM playing at the specified priority level.
    /// </summary>
    /// <param name="priority">The priority level to stop.</param>
    public unsafe void Stop(ushort priority)
    {
        var scenes = GetScenes();
        if (scenes == null)
        {
            return;
        }

        if (priority > LOWEST_PRIORITY)
        {
            return;
        }

        scenes[priority].BgmId = 0;
        scenes[priority].BgmReference = 0;
        scenes[priority].Flags = BgmManager.SceneFlags.None | BgmManager.SceneFlags.ForceAutoReset;
        scenes[priority].Timer = 0;

        Reset();
    }

    /// <summary>
    /// Retrieves the current BGM ID for the specified scene priority.
    /// </summary>
    /// <param name="priority">The scene priority.</param>
    /// <returns>The BGM ID for the specified priority, or 0 if invalid or unavailable.</returns>
    public unsafe ushort CurrentSceneBgmId(ushort priority)
    {
        var scenes = GetScenes();
        if (scenes == null)
        {
            return 0;
        }

        if (priority > LOWEST_PRIORITY)
        {
            return 0;
        }

        return scenes[priority].BgmId;
    }

    /// <summary>
    /// Sets the specified BGM to a scene with the given priority.
    /// </summary>
    /// <param name="priority">The priority level for the scene.</param>
    /// <param name="bgmId">The ID of the BGM to set.</param>
    /// <returns>True if the BGM was successfully set to the specified scene; otherwise, false.</returns>
    public unsafe bool SetBgmToScene(uint priority, ushort bgmId)
    {

        var scenes = GetScenes();
        if (scenes == null)
        {
            return false;
        }

        if (priority > LOWEST_PRIORITY)
        {
            return false;
        }

        scenes[priority].BgmId = bgmId;
        scenes[priority].BgmReference = bgmId;
        // better flags handling is needed in the future
        scenes[priority].Flags = BgmManager.SceneFlags.Resume | BgmManager.SceneFlags.EnableDisableRestart;
        scenes[priority].Timer = 0;

        return true;
    }

    /// <summary>
    /// Resets the BGM context, stopping any active BGM.
    /// </summary>
    public void Reset()
    {
        bgmRestart!(bgmManagerPtr);
    }

    public unsafe void Dispose()
    {
        bgmManagerPtr = IntPtr.Zero;
        baseAddressPtr = IntPtr.Zero;
        bgmManager = IntPtr.Zero;
        scenes = null;
        bgmRestart = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BgmScene
    {
        public int SceneIndex;
        public SceneFlags Flags;
        private int padding1;
        public ushort BgmReference;
        public ushort BgmId;
        public ushort PreviousBgmId;
        public byte TimerEnable;
        private byte padding2;
        public float Timer;
        private fixed byte disableRestartList[24];
        private byte unknown1;
        private uint unknown2;
        private uint unknown3;
        private uint unknown4;
        private uint unknown5;
        private uint unknown6;
        private ulong unknown7;
        private uint unknown8;
        private byte unknown9;
        private byte unknown10;
        private byte unknown11;
        private byte unknown12;
        private float unknown13;
        private uint unknown14;
    }

    [Flags]
    public enum SceneFlags
    {
        None = 0,
        Resume = 1 << 0,
        EnableDisableRestart = 1 << 1,
        ForceAutoReset = 1 << 2,
    }
}
